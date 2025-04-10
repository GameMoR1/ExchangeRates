using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Testing
{
    public class UpdateManager
    {
        private string _githubRepoUrl;
        private string _apkDownloadUrl;

        public UpdateManager(string githubRepoUrl, string apkDownloadUrl)
        {
            _githubRepoUrl = githubRepoUrl;
            _apkDownloadUrl = apkDownloadUrl;
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var lastCheckedHash = Preferences.Get("last_commit_hash", "");
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                var response = await client.GetAsync(_githubRepoUrl + "/commits?per_page=1");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var commits = JsonConvert.DeserializeObject<List<Commit>>(responseBody);

                    if (commits?.Count > 0 && commits[0].Sha != lastCheckedHash)
                    {
                        var update = await App.Current.MainPage.DisplayAlert("Обновление",
                            "Доступна новая версия приложения. Обновить сейчас?",
                            "Да", "Нет");

                        if (update)
                        {
                            await DownloadAndInstallUpdateAsync();
                            Preferences.Set("last_commit_hash", commits[0].Sha);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Update check failed: {ex.Message}");
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

        private class Commit
        {
            public string Sha { get; set; }
        }
    }
}
