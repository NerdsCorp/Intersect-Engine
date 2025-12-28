using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarkUI.Forms;
using Intersect.Compression;
using Intersect.Configuration;
using Intersect.Editor.Core;
using Intersect.Editor.General;
using Intersect.Editor.Localization;
using Intersect.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Editor.Forms;

public partial class FrmUploadToServer : DarkDialog
{
    private string? _selectedDirectory;
    private TokenResponse? _tokenResponse;

    public FrmUploadToServer()
    {
        InitializeComponent();
        Icon = Program.Icon;
        // Automatically set directory to Editor's current directory
        _selectedDirectory = Environment.CurrentDirectory;
        LoadSettings();
        FormClosing += FrmUploadToServer_FormClosing;
    }

    private void FrmUploadToServer_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Save settings when form is closing
        SaveSettings();
    }

    private void LoadSettings()
    {
        var savedUrl = Preferences.LoadPreference("upload_serverUrl");
        if (!string.IsNullOrWhiteSpace(savedUrl))
        {
            txtServerUrl.Text = savedUrl;
        }
        else if (ClientConfiguration.Instance.UpdateUrl is { } updateUrl &&
                 !string.IsNullOrWhiteSpace(updateUrl))
        {
            txtServerUrl.Text = updateUrl;
        }

        var savedType = Preferences.LoadPreference("upload_type");
        rbEditorAssets.Checked = savedType == "editor";
        rbClientAssets.Checked = !rbEditorAssets.Checked;

        var rawTokenResponse = Preferences.LoadPreference(nameof(TokenResponse));
        if (!string.IsNullOrWhiteSpace(rawTokenResponse))
        {
            try
            {
                _tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(rawTokenResponse);
            }
            catch
            {
                _tokenResponse = null;
            }
        }

        UpdateAuthenticationStatus();
    }

    private void UpdateAuthenticationStatus()
    {
        // Check if token is expired
        if (_tokenResponse != null && IsTokenExpired(_tokenResponse))
        {
            _tokenResponse = null;
            Preferences.SavePreference(nameof(TokenResponse), string.Empty);
        }

        if (_tokenResponse != null)
        {
            lblStatus.Text = "✓ Authenticated - Ready to upload";
            btnLogin.Text = "Re-Login";
            btnUpload.Enabled = true;
        }
        else
        {
            lblStatus.Text = "⚠ Not authenticated - Please click the Login button below to authenticate";
            btnLogin.Text = "Login";
            btnUpload.Enabled = false;
        }

        // Login button is always visible now
        btnLogin.Visible = true;

        // Force UI refresh
        btnLogin.Refresh();
        lblStatus.Refresh();
        btnUpload.Refresh();
    }

    private bool IsTokenExpired(TokenResponse token)
    {
        // Token typically expires after a certain period (usually 1 hour in most systems)
        // Since we don't have the issued time, we'll try to parse the JWT to check expiration
        try
        {
            var tokenParts = token.AccessToken.Split('.');
            if (tokenParts.Length != 3)
            {
                return true; // Invalid token format
            }

            // Decode the payload (second part)
            var payload = tokenParts[1];
            // Add padding if needed
            var paddingNeeded = (4 - payload.Length % 4) % 4;
            payload = payload + new string('=', paddingNeeded);

            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var payloadObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);

            if (payloadObject?.TryGetValue("exp", out var expValue) == true)
            {
                var exp = Convert.ToInt64(expValue);
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                return DateTimeOffset.UtcNow >= expirationTime;
            }
        }
        catch
        {
            // If we can't parse the token, consider it expired
            return true;
        }

        // If no expiration claim, assume it's still valid
        return false;
    }

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        // Check if server URL is set
        if (string.IsNullOrWhiteSpace(txtServerUrl.Text))
        {
            DarkMessageBox.ShowError(
                "Please enter a server URL before logging in.",
                "Server URL Required",
                DarkDialogButton.Ok,
                Icon
            );
            return;
        }

        // Prompt for credentials
        using var loginDialog = new Form
        {
            Text = "Login",
            Width = 350,
            Height = 180,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblUsername = new Label { Left = 20, Top = 20, Text = "Username:", Width = 80 };
        var txtUsername = new TextBox { Left = 110, Top = 17, Width = 200 };
        var lblPassword = new Label { Left = 20, Top = 50, Text = "Password:", Width = 80 };
        var txtPassword = new TextBox { Left = 110, Top = 47, Width = 200, PasswordChar = '*' };
        var btnOk = new Button { Text = "Login", Left = 150, Top = 85, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Left = 230, Top = 85, DialogResult = DialogResult.Cancel };

        loginDialog.Controls.AddRange(new Control[] { lblUsername, txtUsername, lblPassword, txtPassword, btnOk, btnCancel });
        loginDialog.AcceptButton = btnOk;
        loginDialog.CancelButton = btnCancel;

        // Load saved credentials if available
        var savedUsername = Preferences.LoadPreference("Username");
        var savedPassword = Preferences.LoadPreference("Password");
        if (!string.IsNullOrWhiteSpace(savedUsername))
        {
            txtUsername.Text = savedUsername;
            if (!string.IsNullOrWhiteSpace(savedPassword))
            {
                txtPassword.Text = "*****";
                txtPassword.Tag = savedPassword; // Store hashed password in Tag
            }
        }

        if (loginDialog.ShowDialog() == DialogResult.OK)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                DarkMessageBox.ShowError(
                    "Username and password are required.",
                    "Invalid Credentials",
                    DarkDialogButton.Ok,
                    Icon
                );
                return;
            }

            // Disable controls during authentication
            btnLogin.Enabled = false;
            lblStatus.Text = "Authenticating...";

            try
            {
                var serverUrl = txtServerUrl.Text.TrimEnd('/');
                using var httpClient = new IntersectHttpClient(serverUrl);

                // Check if we're using a saved password (hashed)
                var isHashedPassword = password == "*****" && txtPassword.Tag is string hashedPwd;
                var passwordToUse = isHashedPassword ? (string)txtPassword.Tag! : password;

                var tokenResult = httpClient.TryRequestToken(
                    username,
                    passwordToUse,
                    out var tokenResponse,
                    hashed: isHashedPassword
                );

                if (tokenResult == TokenResultType.TokenReceived && tokenResponse != null)
                {
                    _tokenResponse = tokenResponse;

                    // Save token to preferences
                    Preferences.SavePreference(nameof(TokenResponse), JsonConvert.SerializeObject(_tokenResponse));

                    // Save credentials for future use
                    Preferences.SavePreference("Username", username);
                    if (!isHashedPassword)
                    {
                        // Hash the password before saving
                        using var sha = SHA256.Create();
                        var hashedPassword = BitConverter.ToString(
                            sha.ComputeHash(Encoding.UTF8.GetBytes(password))
                        ).Replace("-", "");
                        Preferences.SavePreference("Password", hashedPassword);
                    }

                    UpdateAuthenticationStatus();

                    DarkMessageBox.ShowInformation(
                        "Successfully authenticated!",
                        "Login Successful",
                        DarkDialogButton.Ok,
                        Icon
                    );
                }
                else
                {
                    var errorMessage = tokenResult switch
                    {
                        TokenResultType.InvalidCredentials => "Invalid username or password.",
                        TokenResultType.InvalidUsername => "Invalid username.",
                        TokenResultType.InvalidPassword => "Invalid password.",
                        TokenResultType.RequestError => "Network error. Please check your server URL.",
                        _ => $"Authentication failed: {tokenResult}"
                    };

                    DarkMessageBox.ShowError(
                        errorMessage,
                        "Login Failed",
                        DarkDialogButton.Ok,
                        Icon
                    );
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.ShowError(
                    $"Authentication error: {ex.Message}",
                    "Login Error",
                    DarkDialogButton.Ok,
                    Icon
                );
            }
            finally
            {
                btnLogin.Enabled = true;
                if (_tokenResponse == null)
                {
                    UpdateAuthenticationStatus();
                }
            }
        }
    }

    private void SaveSettings()
    {
        Preferences.SavePreference("upload_serverUrl", txtServerUrl.Text);
        Preferences.SavePreference("upload_type", rbEditorAssets.Checked ? "editor" : "client");
    }

    private async void btnUpload_Click(object sender, EventArgs e)
    {
        // Check authentication first
        if (_tokenResponse == null || IsTokenExpired(_tokenResponse))
        {
            DarkMessageBox.ShowError(
                "You must login before uploading.\n\nPlease click the 'Login' button below to authenticate.",
                "Authentication Required",
                DarkDialogButton.Ok,
                Icon
            );
            UpdateAuthenticationStatus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtServerUrl.Text))
        {
            DarkMessageBox.ShowError(
                Strings.UploadToServer.InvalidUrl,
                Strings.UploadToServer.Title,
                DarkDialogButton.Ok,
                Icon
            );
            return;
        }

        SaveSettings();

        // Check if we should package assets before uploading
        var packageUpdateAssets = Preferences.LoadPreference("PackageUpdateAssets");
        if (!string.IsNullOrWhiteSpace(packageUpdateAssets) &&
            Convert.ToBoolean(packageUpdateAssets, CultureInfo.InvariantCulture))
        {
            Globals.PackingProgressForm = new FrmProgress();
            Globals.PackingProgressForm.SetTitle(Strings.AssetPacking.title);
            var assetThread = new Thread(() => FrmMain.packAssets(_selectedDirectory, this));
            assetThread.Start();
            _ = Globals.PackingProgressForm.ShowDialog();
        }

        btnUpload.Enabled = false;
        txtServerUrl.Enabled = false;
        rbClientAssets.Enabled = false;
        rbEditorAssets.Enabled = false;

        progressBar.Value = 0;
        lblStatus.Text = Strings.UploadToServer.Uploading.ToString(0);

        try
        {
            await PerformUpload();
        }
        catch (Exception ex)
        {
            lblStatus.Text = Strings.UploadToServer.Error.ToString(ex.Message);
            DarkMessageBox.ShowError(
                ex.Message,
                Strings.UploadToServer.Failed,
                DarkDialogButton.Ok,
                Icon
            );
        }
        finally
        {
            btnUpload.Enabled = true;
            txtServerUrl.Enabled = true;
            rbClientAssets.Enabled = true;
            rbEditorAssets.Enabled = true;
        }
    }

    private (HashSet<string> excludeFiles, HashSet<string> excludeExtensions, HashSet<string> excludeDirectories, HashSet<string> typeSpecificExcludeFiles, HashSet<string> typeSpecificExcludeDirectories) BuildExclusionLists(string sourceDirectory, bool isEditorUpload, bool packagingEnabled)
    {
        // Base exclusions that apply to both client and editor
        var editorBaseName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();
        var editorFileNameExe = $"{editorBaseName}.exe";
        var editorFileNamePdb = $"{editorBaseName}.pdb";

        string[] excludeFiles =
        [
            "resources/mapcache.db",
            "update.json",
            "version.json",
            "version.client.json",
            "version.editor.json",
            ".gitkeep",
        ];

        string[] excludeExtensions =
        [
            ".dll",
            ".xml",
            ".config",
            ".php",
        ];

        string[] excludeDirectories =
        [
            "logs",
            "screenshots",
        ];

        // Type-specific exclusions
        List<string> typeSpecificExcludeFiles = new();
        List<string> typeSpecificExcludeDirectories = new();

        if (isEditorUpload)
        {
            // Editor upload - exclude client-specific files
            typeSpecificExcludeFiles.Add("resources/client_strings.json");

            // Process packs if they exist
            const string resourcesDirectoryName = "resources";
            var pathToResourcesDirectory = Path.Combine(sourceDirectory, resourcesDirectoryName);
            var pathToPacksDirectory = Path.Combine(pathToResourcesDirectory, "packs");

            if (!packagingEnabled)
            {
                // When packaging is disabled, exclude the packs directory entirely
                typeSpecificExcludeDirectories.Add("resources/packs");

                if (Directory.Exists(pathToPacksDirectory))
                {
                    var packFileNames = Directory.GetFiles(pathToPacksDirectory, "*.meta");
                    typeSpecificExcludeFiles.AddRange(packFileNames.Select(f => Path.GetRelativePath(sourceDirectory, f).Replace('\\', '/')));

                    typeSpecificExcludeFiles.AddRange(
                        packFileNames.SelectMany(pack =>
                        {
                            try
                            {
                                var tokenPack = JToken.Parse(GzipCompression.ReadDecompressedString(pack));
                                if (tokenPack is not JObject objectPack || !objectPack.TryGetValue("frames", out var tokenFrames))
                                {
                                    return Enumerable.Empty<string>();
                                }

                                return tokenFrames.Children()
                                    .OfType<JObject>()
                                    .Where(frameObject => frameObject.TryGetValue("filename", out _))
                                    .Select(frameObject => frameObject["filename"]?.Value<string>())
                                    .Where(filename => !string.IsNullOrWhiteSpace(filename))
                                    .OfType<string>();
                            }
                            catch
                            {
                                return Enumerable.Empty<string>();
                            }
                        })
                    );

                    var soundIndex = Path.Combine(pathToPacksDirectory, "sound.index");
                    if (File.Exists(soundIndex))
                    {
                        typeSpecificExcludeFiles.Add(Path.GetRelativePath(sourceDirectory, soundIndex).Replace('\\', '/'));
                        try
                        {
                            using AssetPacker soundPacker = new(soundIndex, pathToPacksDirectory);
                            typeSpecificExcludeFiles.AddRange(
                                soundPacker.CachedPackages.Select(
                                    cachedPackage => Path.Combine("resources/packs", cachedPackage).Replace('\\', '/')
                                )
                            );
                        }
                        catch
                        {
                            // Ignore packer errors
                        }
                    }

                    var musicIndex = Path.Combine(pathToPacksDirectory, "music.index");
                    if (File.Exists(musicIndex))
                    {
                        typeSpecificExcludeFiles.Add(Path.GetRelativePath(sourceDirectory, musicIndex).Replace('\\', '/'));
                        try
                        {
                            using AssetPacker musicPacker = new(musicIndex, pathToPacksDirectory);
                            typeSpecificExcludeFiles.AddRange(
                                musicPacker.CachedPackages.Select(
                                    cachedPackage => Path.Combine("resources/packs", cachedPackage).Replace('\\', '/')
                                )
                            );
                        }
                        catch
                        {
                            // Ignore packer errors
                        }
                    }
                }
            }
        }
        else
        {
            // Client upload - exclude editor-specific files
            typeSpecificExcludeFiles.Add(editorFileNameExe);
            typeSpecificExcludeFiles.Add(editorFileNamePdb);
            typeSpecificExcludeFiles.Add("resources/editor_strings.json");
            typeSpecificExcludeDirectories.Add("resources/cursors");

            // Process packs to exclude source files
            const string resourcesDirectoryName = "resources";
            var pathToResourcesDirectory = Path.Combine(sourceDirectory, resourcesDirectoryName);
            var pathToPacksDirectory = Path.Combine(pathToResourcesDirectory, "packs");

            if (Directory.Exists(pathToPacksDirectory))
            {
                var packFileNames = Directory.GetFiles(pathToPacksDirectory, "*.meta");
                typeSpecificExcludeFiles.AddRange(
                    packFileNames.SelectMany(pack =>
                    {
                        try
                        {
                            var tokenPack = JToken.Parse(GzipCompression.ReadDecompressedString(pack));
                            if (tokenPack is not JObject objectPack || !objectPack.TryGetValue("frames", out var tokenFrames))
                            {
                                return Enumerable.Empty<string>();
                            }

                            return tokenFrames.Children()
                                .OfType<JObject>()
                                .Where(frameObject => frameObject.TryGetValue("filename", out _))
                                .Select(frameObject => frameObject["filename"]?.Value<string>())
                                .Where(filename => !string.IsNullOrWhiteSpace(filename))
                                .Select(filename => Path.Combine(resourcesDirectoryName, filename!).Replace('\\', '/').ToLower(CultureInfo.CurrentCulture))
                                .OfType<string>();
                        }
                        catch
                        {
                            return Enumerable.Empty<string>();
                        }
                    })
                );

                var soundIndex = Path.Combine(pathToPacksDirectory, "sound.index");
                if (File.Exists(soundIndex))
                {
                    try
                    {
                        using AssetPacker soundPacker = new(soundIndex, pathToPacksDirectory);
                        typeSpecificExcludeFiles.AddRange(
                            soundPacker.FileList.Select(
                                sound => Path.Combine(resourcesDirectoryName, "sounds", sound.ToLower(CultureInfo.CurrentCulture)).Replace('\\', '/')
                            )
                        );
                    }
                    catch
                    {
                        // Ignore packer errors
                    }
                }

                var musicIndex = Path.Combine(pathToPacksDirectory, "music.index");
                if (File.Exists(musicIndex))
                {
                    try
                    {
                        using AssetPacker musicPacker = new(musicIndex, pathToPacksDirectory);
                        typeSpecificExcludeFiles.AddRange(
                            musicPacker.FileList.Select(
                                music => Path.Combine(resourcesDirectoryName, "music", music.ToLower(CultureInfo.CurrentCulture)).Replace('\\', '/')
                            )
                        );
                    }
                    catch
                    {
                        // Ignore packer errors
                    }
                }
            }
        }

        return (
            excludeFiles.ToHashSet(),
            excludeExtensions.ToHashSet(),
            excludeDirectories.ToHashSet(),
            typeSpecificExcludeFiles.ToHashSet(),
            typeSpecificExcludeDirectories.ToHashSet()
        );
    }

    private bool ShouldExcludeFile(string relativePath, string relativeDirectoryPath, string extension,
        HashSet<string> excludeFiles, HashSet<string> excludeExtensions, HashSet<string> excludeDirectories,
        HashSet<string> typeSpecificExcludeFiles, HashSet<string> typeSpecificExcludeDirectories)
    {
        // Normalize paths for comparison
        var normalizedRelativePath = relativePath.Replace('\\', '/');
        var normalizedDirectoryPath = relativeDirectoryPath.Replace('\\', '/');

        // Check if the file's directory is excluded
        if (typeSpecificExcludeDirectories.Any(dir => normalizedDirectoryPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (excludeDirectories.Any(dir => normalizedDirectoryPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check if the file itself is excluded
        if (excludeFiles.Contains(normalizedRelativePath))
        {
            return true;
        }

        if (typeSpecificExcludeFiles.Contains(normalizedRelativePath.ToLower(CultureInfo.CurrentCulture)))
        {
            return true;
        }

        // Check if the extension is excluded
        if (excludeExtensions.Contains(extension))
        {
            return true;
        }

        return false;
    }

    private async Task PerformUpload()
    {
        var uploadType = rbEditorAssets.Checked ? "editor" : "client";
        var isEditorUpload = rbEditorAssets.Checked;
        var serverUrl = txtServerUrl.Text.TrimEnd('/');
        var endpoint = $"{serverUrl}/api/v1/editor/updates/{uploadType}";

        // Check if packaging is enabled
        var packageUpdateAssets = Preferences.LoadPreference("PackageUpdateAssets");
        var packagingEnabled = !string.IsNullOrWhiteSpace(packageUpdateAssets) &&
            Convert.ToBoolean(packageUpdateAssets, CultureInfo.InvariantCulture);

        // Build exclusion lists
        var (excludeFiles, excludeExtensions, excludeDirectories, typeSpecificExcludeFiles, typeSpecificExcludeDirectories) =
            BuildExclusionLists(_selectedDirectory!, isEditorUpload, packagingEnabled);

        // Get all files and filter them
        var allFiles = Directory.GetFiles(
            _selectedDirectory!,
            "*.*",
            SearchOption.AllDirectories
        );

        var files = allFiles.Where(filePath =>
        {
            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(_selectedDirectory!, filePath).Replace('\\', '/');
            var relativeDirectoryPath = Path.GetRelativePath(_selectedDirectory!, fileInfo.DirectoryName ?? _selectedDirectory!).Replace('\\', '/');

            if (relativeDirectoryPath == ".")
            {
                relativeDirectoryPath = "";
            }

            return !ShouldExcludeFile(
                relativePath,
                relativeDirectoryPath,
                fileInfo.Extension,
                excludeFiles,
                excludeExtensions,
                excludeDirectories,
                typeSpecificExcludeFiles,
                typeSpecificExcludeDirectories
            );
        }).ToArray();

        var totalFiles = files.Length;
        var uploadedFiles = 0;

        // Create HTTP client with SSL bypass for localhost
        var handler = new HttpClientHandler();
        var uri = new Uri(serverUrl);
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host == "127.0.0.1" ||
            uri.Host.StartsWith("192.168.") ||
            uri.Host.StartsWith("10.") ||
            uri.Host.StartsWith("172."))
        {
            // Bypass SSL validation for local/private network addresses
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        // Double-check authentication before starting upload
        if (_tokenResponse == null || string.IsNullOrWhiteSpace(_tokenResponse.AccessToken))
        {
            throw new Exception(
                "Cannot upload without authentication. Please login first."
            );
        }

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

        const int batchSize = 10;
        const int maxRetries = 3;

        for (var i = 0; i < files.Length; i += batchSize)
        {
            var batch = files.Skip(i).Take(batchSize).ToArray();
            var attempt = 0;
            Exception? lastException = null;

            while (attempt <= maxRetries)
            {
                var streams = new List<Stream>();
                try
                {
                    using var content = new MultipartFormDataContent();

                    foreach (var filePath in batch)
                    {
                        var relativePath = Path
                            .GetRelativePath(_selectedDirectory!, filePath)
                            .Replace('\\', '/');

                        var stream = File.OpenRead(filePath);
                        streams.Add(stream);
                        var fileContent = new StreamContent(stream);
                        fileContent.Headers.ContentType =
                            MediaTypeHeaderValue.Parse("application/octet-stream");

                        content.Add(fileContent, "files", relativePath);
                    }

                    var response = await httpClient.PostAsync(endpoint, content);

                    if (response.StatusCode ==
                        System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Clear the invalid token
                        _tokenResponse = null;
                        Preferences.SavePreference(nameof(TokenResponse), string.Empty);
                        Invoke(new Action(UpdateAuthenticationStatus));

                        throw new Exception(
                            "Authentication failed. Your session may have expired. Please login again."
                        );
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception(
                            $"Upload failed ({response.StatusCode}): {error}"
                        );
                    }

                    uploadedFiles += batch.Length;
                    var progress = (int)(
                        uploadedFiles / (float)totalFiles * 100
                    );

                    progressBar.Value = Math.Min(progress, 100);
                    lblStatus.Text =
                        Strings.UploadToServer.FilesUploaded
                            .ToString(uploadedFiles, totalFiles);

                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt <= maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        lblStatus.Text =
                            $"Retrying batch ({attempt}/{maxRetries})...";
                        await Task.Delay(delay);
                    }
                }
                finally
                {
                    // Ensure all streams are disposed
                    foreach (var stream in streams)
                    {
                        stream?.Dispose();
                    }
                }
            }

            // If all retries failed, throw the last exception
            if (lastException != null && attempt > maxRetries)
            {
                throw lastException;
            }
        }

        progressBar.Value = 100;
        lblStatus.Text = Strings.UploadToServer.Success;

        DarkMessageBox.ShowInformation(
            Strings.UploadToServer.Success,
            Strings.UploadToServer.Completed,
            DarkDialogButton.Ok,
            Icon
        );
    }

    private async void btnTestUrl_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtServerUrl.Text))
        {
            DarkMessageBox.ShowError(
                "Please enter a server URL to test.",
                "Server URL Required",
                DarkDialogButton.Ok,
                Icon
            );
            return;
        }

        btnTestUrl.Enabled = false;
        var originalStatus = lblStatus.Text;
        lblStatus.Text = "Testing server URL...";

        try
        {
            var serverUrl = txtServerUrl.Text.TrimEnd('/');

            // Create HTTP client with SSL bypass for localhost
            var handler = new HttpClientHandler();
            var uri = new Uri(serverUrl);
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host == "127.0.0.1" ||
                uri.Host.StartsWith("192.168.") ||
                uri.Host.StartsWith("10.") ||
                uri.Host.StartsWith("172."))
            {
                // Bypass SSL validation for local/private network addresses
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            using var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            // Try to reach the server's API endpoint
            var testEndpoint = $"{serverUrl}/api/v1/info";
            var response = await httpClient.GetAsync(testEndpoint);

            if (response.IsSuccessStatusCode)
            {
                lblStatus.Text = "✓ Server URL is reachable";
                DarkMessageBox.ShowInformation(
                    "The server URL is valid and reachable!",
                    "Test Successful",
                    DarkDialogButton.Ok,
                    Icon
                );
            }
            else
            {
                lblStatus.Text = $"⚠ Server responded with status: {response.StatusCode}";
                DarkMessageBox.ShowWarning(
                    $"The server responded but returned status code: {response.StatusCode}\n\n" +
                    "The URL may still work for uploading if the API is configured correctly.",
                    "Server Response",
                    DarkDialogButton.Ok,
                    Icon
                );
            }
        }
        catch (HttpRequestException ex)
        {
            lblStatus.Text = "✗ Failed to reach server";
            DarkMessageBox.ShowError(
                $"Could not connect to the server:\n\n{ex.Message}\n\n" +
                "Please check the URL and ensure the server is running.",
                "Connection Failed",
                DarkDialogButton.Ok,
                Icon
            );
        }
        catch (TaskCanceledException)
        {
            lblStatus.Text = "✗ Connection timed out";
            DarkMessageBox.ShowError(
                "The connection to the server timed out.\n\n" +
                "Please check the URL and ensure the server is running.",
                "Connection Timeout",
                DarkDialogButton.Ok,
                Icon
            );
        }
        catch (Exception ex)
        {
            lblStatus.Text = "✗ Test failed";
            DarkMessageBox.ShowError(
                $"An error occurred while testing the URL:\n\n{ex.Message}",
                "Test Error",
                DarkDialogButton.Ok,
                Icon
            );
        }
        finally
        {
            btnTestUrl.Enabled = true;
            // Restore authentication status after a delay
            await Task.Delay(3000);
            UpdateAuthenticationStatus();
        }
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
    }
}
