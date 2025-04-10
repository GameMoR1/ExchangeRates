using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Testing
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private string _baseCurrency = "USD"; // Базовая валюта по умолчанию

        private string _githubRepoUrl = "https://api.github.com/repos/YOUR_GITHUB_USERNAME/YOUR_REPO_NAME";
        private string _apkDownloadUrl = "https://github.com/YOUR_GITHUB_USERNAME/YOUR_REPO_NAME/releases/latest/download/app.apk";

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            LoadCurrenciesAsync();
            //CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            await Task.Run(async () =>
            {
                var updateManager = new UpdateManager(_githubRepoUrl, _apkDownloadUrl);
                await updateManager.CheckForUpdatesAsync();
            });
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
