using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
            btnLogin.Visible = false;
        }
        else
        {
            lblStatus.Text = "⚠ Not authenticated - click Login to authenticate";
            btnLogin.Visible = true;
        }
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
