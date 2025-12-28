using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using Intersect.Server.Web.Http;
using Intersect.Server.Web.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Intersect.Server.Web.Controllers.Api;

/// <summary>
/// API endpoints for chunked file uploads from the editor to the server.
/// Supports large files (multi-GB) with resume capability.
/// </summary>
[Route("api/v1/editor/chunked-upload")]
[ApiController]
[Authorize(Policy = "Developer")]
public sealed class ChunkedUploadController : ControllerBase
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ChunkedUploadController> _logger;
    private readonly IOptionsMonitor<UpdateServerOptions> _updateServerOptionsMonitor;

    // In-memory storage for upload sessions
    // In production, consider using distributed cache (Redis) for multiple server instances
    private static readonly ConcurrentDictionary<string, UploadSession> _uploadSessions = new();
    private static readonly object _sessionLock = new();

    public ChunkedUploadController(
        IHostEnvironment hostEnvironment,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<UpdateServerOptions> updateServerOptionsMonitor
    )
    {
        _hostEnvironment = hostEnvironment;
        _logger = loggerFactory.CreateLogger<ChunkedUploadController>();
        _updateServerOptionsMonitor = updateServerOptionsMonitor;
    }

    private string AssetRootPath => Path.Combine(
        _hostEnvironment.ContentRootPath,
        _updateServerOptionsMonitor.CurrentValue.AssetRoot
    );

    /// <summary>
    /// Initialize a chunked upload session for a file.
    /// </summary>
    [HttpPost("init")]
    [ProducesResponseType(typeof(InitUploadResponse), (int)HttpStatusCode.OK, ContentTypes.Json)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest, ContentTypes.Json)]
    public IActionResult InitializeUpload([FromBody] InitUploadRequest request)
    {
        if (!_updateServerOptionsMonitor.CurrentValue.Enabled)
        {
            return NotFound(new ErrorResponse { Error = "Update server is not enabled" });
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest(new ErrorResponse { Error = "File name is required" });
        }

        if (request.TotalSize <= 0)
        {
            return BadRequest(new ErrorResponse { Error = "Total size must be greater than 0" });
        }

        if (request.ChunkSize <= 0 || request.ChunkSize > 50_000_000) // Max 50MB per chunk
        {
            return BadRequest(new ErrorResponse { Error = "Chunk size must be between 1 and 50MB" });
        }

        var uploadType = request.UploadType?.ToLowerInvariant();
        if (uploadType != "client" && uploadType != "editor")
        {
            return BadRequest(new ErrorResponse { Error = "Upload type must be 'client' or 'editor'" });
        }

        // Generate unique upload session ID
        var sessionId = Guid.NewGuid().ToString();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Calculate total chunks
        var totalChunks = (int)Math.Ceiling((double)request.TotalSize / request.ChunkSize);

        // Create temp directory for chunks
        var tempDir = Path.Combine(Path.GetTempPath(), "intersect-uploads", sessionId);
        Directory.CreateDirectory(tempDir);

        var session = new UploadSession
        {
            SessionId = sessionId,
            UserId = userId ?? "unknown",
            FileName = request.FileName,
            RelativePath = request.RelativePath ?? string.Empty,
            UploadType = uploadType,
            TotalSize = request.TotalSize,
            ChunkSize = request.ChunkSize,
            TotalChunks = totalChunks,
            TempDirectory = tempDir,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _uploadSessions[sessionId] = session;

        _logger.LogInformation(
            "Initialized chunked upload session {SessionId} for {FileName} ({TotalSize:N0} bytes, {TotalChunks} chunks)",
            sessionId, request.FileName, request.TotalSize, totalChunks
        );

        return Ok(new InitUploadResponse
        {
            SessionId = sessionId,
            TotalChunks = totalChunks,
            ChunkSize = request.ChunkSize
        });
    }

    /// <summary>
    /// Upload a single chunk of a file.
    /// </summary>
    [HttpPost("chunk")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    [ProducesResponseType(typeof(ChunkUploadResponse), (int)HttpStatusCode.OK, ContentTypes.Json)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest, ContentTypes.Json)]
    public async Task<IActionResult> UploadChunk([FromForm] string sessionId, [FromForm] int chunkIndex)
    {
        if (!_uploadSessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(new ErrorResponse { Error = "Upload session not found or expired" });
        }

        if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
        {
            return BadRequest(new ErrorResponse { Error = $"Invalid chunk index: {chunkIndex}" });
        }

        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse { Error = "No file data provided" });
        }

        // Verify chunk hasn't been uploaded already
        if (session.UploadedChunks.Contains(chunkIndex))
        {
            _logger.LogWarning("Chunk {ChunkIndex} already uploaded for session {SessionId}", chunkIndex, sessionId);
            return Ok(new ChunkUploadResponse
            {
                ChunkIndex = chunkIndex,
                UploadedChunks = session.UploadedChunks.Count,
                TotalChunks = session.TotalChunks,
                IsComplete = session.UploadedChunks.Count == session.TotalChunks
            });
        }

        // Save chunk to temp directory
        var chunkPath = Path.Combine(session.TempDirectory, $"chunk_{chunkIndex:D6}");
        try
        {
            using var fileStream = System.IO.File.Create(chunkPath);
            await file.CopyToAsync(fileStream);

            lock (_sessionLock)
            {
                session.UploadedChunks.Add(chunkIndex);
                session.LastActivityAt = DateTime.UtcNow;
            }

            _logger.LogDebug(
                "Uploaded chunk {ChunkIndex}/{TotalChunks} for session {SessionId}",
                chunkIndex, session.TotalChunks, sessionId
            );

            return Ok(new ChunkUploadResponse
            {
                ChunkIndex = chunkIndex,
                UploadedChunks = session.UploadedChunks.Count,
                TotalChunks = session.TotalChunks,
                IsComplete = session.UploadedChunks.Count == session.TotalChunks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            return StatusCode(500, new ErrorResponse { Error = $"Failed to save chunk: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get the status of an upload session (for resume capability).
    /// </summary>
    [HttpGet("status/{sessionId}")]
    [ProducesResponseType(typeof(UploadStatusResponse), (int)HttpStatusCode.OK, ContentTypes.Json)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound, ContentTypes.Json)]
    public IActionResult GetUploadStatus(string sessionId)
    {
        if (!_uploadSessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(new ErrorResponse { Error = "Upload session not found or expired" });
        }

        return Ok(new UploadStatusResponse
        {
            SessionId = sessionId,
            FileName = session.FileName,
            UploadedChunks = session.UploadedChunks.OrderBy(c => c).ToList(),
            TotalChunks = session.TotalChunks,
            IsComplete = session.UploadedChunks.Count == session.TotalChunks
        });
    }

    /// <summary>
    /// Finalize an upload by assembling all chunks into the final file.
    /// </summary>
    [HttpPost("finalize")]
    [ProducesResponseType(typeof(FinalizeUploadResponse), (int)HttpStatusCode.OK, ContentTypes.Json)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest, ContentTypes.Json)]
    public async Task<IActionResult> FinalizeUpload([FromBody] FinalizeUploadRequest request)
    {
        if (!_uploadSessions.TryGetValue(request.SessionId, out var session))
        {
            return NotFound(new ErrorResponse { Error = "Upload session not found or expired" });
        }

        // Verify all chunks are uploaded
        if (session.UploadedChunks.Count != session.TotalChunks)
        {
            return BadRequest(new ErrorResponse
            {
                Error = $"Not all chunks uploaded: {session.UploadedChunks.Count}/{session.TotalChunks}"
            });
        }

        try
        {
            // Construct destination path
            var assetRootPath = AssetRootPath;
            var relativePath = string.IsNullOrWhiteSpace(session.RelativePath)
                ? session.UploadType
                : Path.Combine(session.UploadType, session.RelativePath.Trim().Trim('/').Trim('\\'));

            var destinationFolder = Path.GetFullPath(Path.Combine(assetRootPath, relativePath));
            var relativeDestinationFolder = Path.GetRelativePath(assetRootPath, destinationFolder);

            // Security: Ensure uploads stay within the asset root
            if (relativeDestinationFolder.StartsWith(".."))
            {
                _logger.LogWarning(
                    "{UserId} tried to upload to a folder outside of the sandbox: {DirectoryPath}",
                    session.UserId, destinationFolder
                );
                return StatusCode(
                    (int)HttpStatusCode.Forbidden,
                    new ErrorResponse { Error = "Invalid destination path" }
                );
            }

            // Create destination directory if needed
            Directory.CreateDirectory(destinationFolder);

            // Assemble chunks into final file
            var finalFilePath = Path.Combine(destinationFolder, session.FileName);
            await AssembleChunks(session, finalFilePath);

            var fileInfo = new FileInfo(finalFilePath);
            var relativeToAssetRoot = Path.GetRelativePath(assetRootPath, finalFilePath);

            _logger.LogInformation(
                "Finalized chunked upload {SessionId}: {FileName} ({Size:N0} bytes) to {Path}",
                session.SessionId, session.FileName, fileInfo.Length, relativeToAssetRoot
            );

            // Clean up temp files and session
            CleanupSession(session);

            return Ok(new FinalizeUploadResponse
            {
                Success = true,
                FileName = session.FileName,
                Size = fileInfo.Length,
                Path = relativeToAssetRoot
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finalize upload for session {SessionId}", session.SessionId);
            return StatusCode(500, new ErrorResponse { Error = $"Failed to finalize upload: {ex.Message}" });
        }
    }

    /// <summary>
    /// Cancel an upload session and clean up temporary files.
    /// </summary>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound, ContentTypes.Json)]
    public IActionResult CancelUpload(string sessionId)
    {
        if (!_uploadSessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(new ErrorResponse { Error = "Upload session not found" });
        }

        CleanupSession(session);

        _logger.LogInformation("Cancelled upload session {SessionId}", sessionId);

        return Ok();
    }

    private async Task AssembleChunks(UploadSession session, string destinationPath)
    {
        using var outputStream = System.IO.File.Create(destinationPath);

        // Assemble chunks in order
        for (int i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = Path.Combine(session.TempDirectory, $"chunk_{i:D6}");
            if (!System.IO.File.Exists(chunkPath))
            {
                throw new FileNotFoundException($"Chunk {i} not found", chunkPath);
            }

            using var chunkStream = System.IO.File.OpenRead(chunkPath);
            await chunkStream.CopyToAsync(outputStream);
        }
    }

    private void CleanupSession(UploadSession session)
    {
        try
        {
            if (Directory.Exists(session.TempDirectory))
            {
                Directory.Delete(session.TempDirectory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temp directory for session {SessionId}", session.SessionId);
        }

        _uploadSessions.TryRemove(session.SessionId, out _);
    }
}

#region Models

public class UploadSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string UploadType { get; set; } = string.Empty; // "client" or "editor"
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }
    public int TotalChunks { get; set; }
    public HashSet<int> UploadedChunks { get; set; } = new();
    public string TempDirectory { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public class InitUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public string? RelativePath { get; set; }
    public string UploadType { get; set; } = string.Empty; // "client" or "editor"
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; } = 10_000_000; // Default 10MB
}

public class InitUploadResponse
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalChunks { get; set; }
    public int ChunkSize { get; set; }
}

public class ChunkUploadResponse
{
    public int ChunkIndex { get; set; }
    public int UploadedChunks { get; set; }
    public int TotalChunks { get; set; }
    public bool IsComplete { get; set; }
}

public class UploadStatusResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public List<int> UploadedChunks { get; set; } = new();
    public int TotalChunks { get; set; }
    public bool IsComplete { get; set; }
}

public class FinalizeUploadRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public class FinalizeUploadResponse
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Path { get; set; } = string.Empty;
}

#endregion
