using RegistroPonto.Models;
using RegistroPonto.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;

namespace RegistroPonto.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly RegistroService _registroService;
        private List<Models.RegistroPonto> _registros;

        public List<Models.RegistroPonto> Registros
        {
            get => _registros;
            set
            {
                _registros = value;
                OnPropertyChanged();
            }
        }

        private string _horasTrabalhadas;
        public string HorasTrabalhadas
        {
            get => _horasTrabalhadas;
            set
            {
                _horasTrabalhadas = value;
                OnPropertyChanged();
            }
        }

        public ICommand RegistrarEntradaCommand { get; }
        public ICommand RegistrarSaidaCommand { get; }
        public ICommand CalcularHorasCommand { get; }
        public ICommand ExportarRelatorioCommand { get; }

        public MainPageViewModel()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "registros.db");
            _registroService = new RegistroService(dbPath);
            CarregarRegistrosAsync();

            RegistrarEntradaCommand = new Command(async () => await RegistrarEntradaAsync());
            RegistrarSaidaCommand = new Command(async () => await RegistrarSaidaAsync());
            CalcularHorasCommand = new Command(async () => await CalcularHorasAsync());
            ExportarRelatorioCommand = new Command(async () => await ExportarRelatorioAsync());
        }
        private async Task CarregarRegistrosAsync()
        {
            Registros = await _registroService.ObterRegistrosAsync();
        }

        private async Task RegistrarEntradaAsync()
        {
            try
            {
                await _registroService.AdicionarRegistroAsync("Entrada");
                await CarregarRegistrosAsync();
            }
            catch (InvalidOperationException ex)
            {
                await App.Current.MainPage.DisplayAlert("Aviso", ex.Message, "OK");
            }
        }

        private async Task RegistrarSaidaAsync()
        {
            try
            {
                await _registroService.AdicionarRegistroAsync("Saída");
                await CarregarRegistrosAsync();
            }
            catch (InvalidOperationException ex)
            {
                await App.Current.MainPage.DisplayAlert("Aviso", ex.Message, "OK");
            }
        }
        private async Task CalcularHorasAsync()
        {
            var horas = await _registroService.CalcularHorasTrabalhadasAsync();
            HorasTrabalhadas = $"Horas trabalhadas: {horas:hh\\:mm\\:ss}";
        }
        private async Task ExportarRelatorioAsync()
        {
            try
            {
                var filePath = await _registroService.ExportarParaCsvAsync();
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Exportar Registros",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}