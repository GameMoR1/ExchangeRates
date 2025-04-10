using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Testing
{
    public class UpdateManager
    {
        private string _githubRepoUrl;
        private string _apkDownloadUrl;
        private string _currentVersion;

        public UpdateManager(string githubRepoUrl, string apkDownloadUrl, string currentVersion)
        {
            _githubRepoUrl = githubRepoUrl;
            _apkDownloadUrl = apkDownloadUrl;
            _currentVersion = currentVersion;
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                // Для примера, предположим, что версия приложения хранится в файле на GitHub
                var response = await client.GetAsync(_githubRepoUrl + "/version.txt");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var latestVersion = responseBody.Trim();

                    if (latestVersion != _currentVersion)
                    {
                        var update = await App.Current.MainPage.DisplayAlert("Обновление",
                            "Доступна новая версия приложения. Обновить сейчас?",
                            "Да", "Нет");

                        if (update)
                        {
                            await DownloadAndInstallUpdateAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private async Task DownloadAndInstallUpdateAsync()
        {
            try
            {
                var permissionStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();

                if (permissionStatus != PermissionStatus.Granted)
                {
                    permissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }

                if (permissionStatus == PermissionStatus.Granted)
                {
                    var httpClient = new HttpClient();
                    var apkBytes = await httpClient.GetByteArrayAsync(_apkDownloadUrl);

                    var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");
                    var apkPath = Path.Combine(downloadsPath, "update.apk");

                    File.WriteAllBytes(apkPath, apkBytes);

                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(apkPath)
                    });
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось установить обновление: {ex.Message}", "OK");
            }
        }
    }
}
