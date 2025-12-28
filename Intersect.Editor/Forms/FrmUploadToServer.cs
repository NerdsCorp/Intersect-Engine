using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarkUI.Forms;
using Intersect.Configuration;
using Intersect.Editor.Core;
using Intersect.Editor.General;
using Intersect.Editor.Localization;
using Intersect.Web;
using Newtonsoft.Json;

namespace Intersect.Editor.Forms;

public partial class FrmUploadToServer : DarkDialog
{
    private string? _selectedDirectory;
    private TokenResponse? _tokenResponse;

    public FrmUploadToServer()
    {
        InitializeComponent();
        Icon = Program.Icon;
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

        var savedDirectory = Preferences.LoadPreference("upload_lastDirectory");
        if (!string.IsNullOrWhiteSpace(savedDirectory) && Directory.Exists(savedDirectory))
        {
            _selectedDirectory = savedDirectory;
            txtDirectory.Text = savedDirectory;
            btnUpload.Enabled = true;
        }

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
        if (_tokenResponse != null)
        {
            lblStatus.Text = "✓ Authenticated";
        }
        else
        {
            lblStatus.Text = "⚠ Not authenticated - login through the editor first";
        }
    }

    private void SaveSettings()
    {
        Preferences.SavePreference("upload_serverUrl", txtServerUrl.Text);
        Preferences.SavePreference("upload_type", rbEditorAssets.Checked ? "editor" : "client");

        if (!string.IsNullOrWhiteSpace(_selectedDirectory))
        {
            Preferences.SavePreference("upload_lastDirectory", _selectedDirectory);
        }
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog
        {
            Description = Strings.UploadToServer.SourceDirectoryPrompt,
            ShowNewFolderButton = false
        };

        var lastDir = Preferences.LoadPreference("upload_lastDirectory");
        if (!string.IsNullOrWhiteSpace(lastDir) && Directory.Exists(lastDir))
        {
            folderDialog.SelectedPath = lastDir;
        }

        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            _selectedDirectory = folderDialog.SelectedPath;
            txtDirectory.Text = _selectedDirectory;
            btnUpload.Enabled = true;
        }
    }

    private async void btnUpload_Click(object sender, EventArgs e)
    {
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

        if (string.IsNullOrWhiteSpace(_selectedDirectory) ||
            !Directory.Exists(_selectedDirectory))
        {
            DarkMessageBox.ShowError(
                Strings.UploadToServer.InvalidDirectory,
                Strings.UploadToServer.Title,
                DarkDialogButton.Ok,
                Icon
            );
            return;
        }

        SaveSettings();

        btnUpload.Enabled = false;
        btnBrowse.Enabled = false;
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
            btnBrowse.Enabled = true;
            txtServerUrl.Enabled = true;
            rbClientAssets.Enabled = true;
            rbEditorAssets.Enabled = true;
        }
    }

    private async Task PerformUpload()
    {
        var uploadType = rbEditorAssets.Checked ? "editor" : "client";
        var serverUrl = txtServerUrl.Text.TrimEnd('/');
        var endpoint = $"{serverUrl}/api/v1/editor/updates/{uploadType}";

        var files = Directory.GetFiles(
            _selectedDirectory!,
            "*.*",
            SearchOption.AllDirectories
        );

        var totalFiles = files.Length;
        var uploadedFiles = 0;

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        if (_tokenResponse != null)
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);
        }

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
                        throw new Exception(
                            "Authentication required. Please ensure you have logged in with developer credentials."
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

    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
    }
}
