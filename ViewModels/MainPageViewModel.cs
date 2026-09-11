using RegistroPonto.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;

namespace RegistroPonto.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly RegistroService _registroService;

        public ObservableCollection<Models.RegistroPonto> Registros { get; } = new();

        private string _horasTrabalhadas = "00:00:00";
        public string HorasTrabalhadas
        {
            get => _horasTrabalhadas;
            set { _horasTrabalhadas = value; OnPropertyChanged(); }
        }

        public ICommand RegistrarEntradaCommand { get; }
        public ICommand RegistrarSaidaCommand { get; }
        public ICommand CalcularHorasCommand { get; }
        public ICommand ExportarRelatorioCommand { get; }
        public ICommand AtualizarCommand { get; }

        public MainPageViewModel()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "registros.db");
            _registroService = new RegistroService(dbPath);

            RegistrarEntradaCommand = new Command(async () => await RegistrarEntradaAsync());
            RegistrarSaidaCommand = new Command(async () => await RegistrarSaidaAsync());
            CalcularHorasCommand = new Command(async () => await CalcularHorasAsync());
            ExportarRelatorioCommand = new Command(async () => await ExportarRelatorioAsync());
            AtualizarCommand = new Command(async () => await CarregarRegistrosAsync());

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            await CarregarRegistrosAsync();
            await CalcularHorasAsync();
        }

        private async Task CarregarRegistrosAsync()
        {
            var registros = await _registroService.ObterRegistrosAsync();
            Registros.Clear();
            foreach (var registro in registros)
                Registros.Add(registro);
        }

        private async Task RegistrarEntradaAsync()
        {
            try
            {
                await _registroService.AdicionarRegistroAsync("Entrada");
                await CarregarRegistrosAsync();
                await CalcularHorasAsync();
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
                await CalcularHorasAsync();
            }
            catch (InvalidOperationException ex)
            {
                await App.Current.MainPage.DisplayAlert("Aviso", ex.Message, "OK");
            }
        }

        private async Task CalcularHorasAsync()
        {
            var horas = await _registroService.CalcularHorasTrabalhadasAsync();
            HorasTrabalhadas = horas.ToString(@"hh\:mm\:ss");
        }

        private async Task ExportarRelatorioAsync()
        {
            try
            {
                var filePath = await _registroService.ExportarParaCsvAsync();
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Exportar registros de ponto",
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