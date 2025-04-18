﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace Testing
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private string _baseCurrency = "USD"; // Базовая валюта по умолчанию

        private string _githubRepoUrl = "https://github.com/GameMoR1/ExchangeRates/tree/main";
        private string _apkDownloadUrl = "https://github.com/GameMoR1/ExchangeRates/blob/main/Testing/Testing.Android/bin/Release/com.companyname.testing-Signed.apk";
        private string version = "0.1.2";

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            RequestPermissionsAsync().ContinueWith((task) =>
            {
                ShowUpdateAnimation();

                CheckForUpdates().ContinueWith((updateTask) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        HideUpdateAnimation();
                    });
                });
            });
        }

        private async Task CheckForUpdates()
        {
            try
            {
                var updateManager = new UpdateManager(_githubRepoUrl, _apkDownloadUrl, version);
                await updateManager.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                CounterLabel.Text = ($"Ошибка обновления: {ex.Message}");
            }
        }

        private async Task RequestPermissionsAsync()
        {
            try
            {
                // Запрашиваем доступ к хранилищу
                var storagePermissionStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (storagePermissionStatus != PermissionStatus.Granted)
                {
                    var newStoragePermissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    if (newStoragePermissionStatus != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Ошибка", "Не удалось получить доступ к хранилищу.", "OK");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                CounterLabel.Text = ($"Ошибка запроса прав: {ex.Message}");
            }
        }


        private void ShowUpdateAnimation()
        {
            // Добавьте анимацию здесь, например, ActivityIndicator
            updateActivityIndicator.IsVisible = true;
            updateActivityIndicator.IsRunning = true;
        }

        private void HideUpdateAnimation()
        {
            // Уберите анимацию здесь
            updateActivityIndicator.IsVisible = false;
            updateActivityIndicator.IsRunning = false;
        }

        private List<string> _currencyList = new List<string>();
        private List<string> _filteredCurrencyList = new List<string>();
        public List<string> FilteredCurrencyList
        {
            get => _filteredCurrencyList;
            set
            {
                _filteredCurrencyList = value;
                OnPropertyChanged(nameof(FilteredCurrencyList));
            }
        }

        private string _selectedCurrency;
        public string SelectedCurrency
        {
            get => _selectedCurrency;
            set
            {
                _selectedCurrency = value;
                OnPropertyChanged(nameof(SelectedCurrency));
                UpdateLabel();
            }
        }

        private Dictionary<string, double> _exchangeRates;

        private async Task LoadCurrenciesAsync()
        {
            try
            {
                string apiUrl = "https://api.currencyfreaks.com/latest?apikey=b8b3e3cb7cc3490fbbb5dfd21f8dc4d2";
                var client = new HttpClient();
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var exchangeRates = JsonConvert.DeserializeObject<ExchangeRateResponse>(responseBody);

                    _baseCurrency = exchangeRates.Base;
                    _currencyList = exchangeRates.Rates.Keys.OrderBy(c => c).ToList();
                    FilteredCurrencyList = new List<string>(_currencyList);
                    _exchangeRates = exchangeRates.Rates;
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось загрузить курсы валют", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void UpdateLabel()
        {
            if (!string.IsNullOrEmpty(SelectedCurrency) &&
                _exchangeRates != null &&
                _exchangeRates.TryGetValue(SelectedCurrency, out double rate))
            {
                if (double.TryParse(AmountEntry.Text, out double amount))
                {
                    CounterLabel.Text = $"{amount} {_baseCurrency} = {amount * rate:F4} {SelectedCurrency}";
                }
                else
                {
                    CounterLabel.Text = $"1 {_baseCurrency} = {rate:F4} {SelectedCurrency}";
                }
            }
            else
            {
                CounterLabel.Text = "Курс не найден.";
            }
        }

        private void BaseCurrencyPicker_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CurrencyPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLabel();
        }

        private async void SearchButton_Clicked(object sender, EventArgs e)
        {
            var searchText = SearchEntry.Text?.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                await DisplayAlert("Внимание", "Введите код валюты", "OK");
                return;
            }

            if (_exchangeRates == null)
            {
                await DisplayAlert("Ошибка", "Данные о курсах не загружены", "OK");
                return;
            }

            if (_exchangeRates.TryGetValue(searchText, out double rate))
            {
                SelectedCurrency = searchText;
                UpdateLabel();
            }
            else
            {
                await DisplayAlert("Ошибка", "Валюта не найдена", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class ExchangeRateResponse
        {
            public string Base { get; set; }
            public DateTime Date { get; set; }
            public Dictionary<string, double> Rates { get; set; }
        }
    }
}
