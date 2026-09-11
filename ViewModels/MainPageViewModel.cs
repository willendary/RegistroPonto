using RegistroPonto.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Globalization;

namespace RegistroPonto.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private const double JornadaEsperadaHoras = 8;
        private readonly RegistroService _registroService;
        private DateTime _dataSelecionada = DateTime.Today;
        private string _horasTrabalhadas = "00:00";
        private string _saldoDia = "00:00";
        private string _statusJornada = "Fora da jornada";
        private string _totalMes = "00:00";
        private string _diasTrabalhados = "0";
        private string _mediaDiaria = "00:00";
        private string _saldoMes = "00:00";

        public ObservableCollection<Models.RegistroPonto> Registros { get; } = new();

        public DateTime DataSelecionada
        {
            get => _dataSelecionada;
            set
            {
                if (_dataSelecionada == value.Date)
                    return;

                _dataSelecionada = value.Date;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataSelecionadaTexto));
                _ = AtualizarPainelAsync();
            }
        }

        public string DataSelecionadaTexto =>
            DataSelecionada.Date == DateTime.Today
                ? "Hoje"
                : DataSelecionada.ToString("dddd, dd 'de' MMMM", new CultureInfo("pt-BR"));

        public string HorasTrabalhadas
        {
            get => _horasTrabalhadas;
            set { _horasTrabalhadas = value; OnPropertyChanged(); }
        }

        public string SaldoDia
        {
            get => _saldoDia;
            set { _saldoDia = value; OnPropertyChanged(); }
        }

        public string StatusJornada
        {
            get => _statusJornada;
            set { _statusJornada = value; OnPropertyChanged(); }
        }

        public string TotalMes
        {
            get => _totalMes;
            set { _totalMes = value; OnPropertyChanged(); }
        }

        public string DiasTrabalhados
        {
            get => _diasTrabalhados;
            set { _diasTrabalhados = value; OnPropertyChanged(); }
        }

        public string MediaDiaria
        {
            get => _mediaDiaria;
            set { _mediaDiaria = value; OnPropertyChanged(); }
        }

        public string SaldoMes
        {
            get => _saldoMes;
            set { _saldoMes = value; OnPropertyChanged(); }
        }

        public ICommand RegistrarEntradaCommand { get; }
        public ICommand RegistrarSaidaCommand { get; }
        public ICommand CalcularHorasCommand { get; }
        public ICommand ExportarRelatorioCommand { get; }
        public ICommand AtualizarCommand { get; }
        public ICommand DiaAnteriorCommand { get; }
        public ICommand ProximoDiaCommand { get; }
        public ICommand HojeCommand { get; }
        public ICommand EditarRegistroCommand { get; }
        public ICommand ExcluirRegistroCommand { get; }

        public MainPageViewModel()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "registros.db");

            _registroService = new RegistroService(dbPath);

            RegistrarEntradaCommand = new Command(async () => await RegistrarEntradaAsync());
            RegistrarSaidaCommand = new Command(async () => await RegistrarSaidaAsync());
            CalcularHorasCommand = new Command(async () => await AtualizarPainelAsync());
            ExportarRelatorioCommand = new Command(async () => await ExportarRelatorioAsync());
            AtualizarCommand = new Command(async () => await AtualizarPainelAsync());
            DiaAnteriorCommand = new Command(async () => await AlterarDataAsync(-1));
            ProximoDiaCommand = new Command(async () => await AlterarDataAsync(1));
            HojeCommand = new Command(async () =>
            {
                DataSelecionada = DateTime.Today;
                await AtualizarPainelAsync();
            });
            EditarRegistroCommand = new Command<Models.RegistroPonto>(async registro => await EditarRegistroAsync(registro));
            ExcluirRegistroCommand = new Command<Models.RegistroPonto>(async registro => await ExcluirRegistroAsync(registro));

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            await AtualizarPainelAsync();
        }

        private async Task AlterarDataAsync(int dias)
        {
            DataSelecionada = DataSelecionada.AddDays(dias);
        }

        private async Task AtualizarPainelAsync()
        {
            var registros = await _registroService.ObterRegistrosPorDataAsync(DataSelecionada);
            Registros.Clear();

            foreach (var registro in registros)
                Registros.Add(registro);

            await CalcularResumoDiaAsync();
            await CalcularResumoMesAsync();
        }

        private async Task CalcularResumoDiaAsync()
        {
            var horas = await _registroService.CalcularHorasTrabalhadasAsync(DataSelecionada);
            HorasTrabalhadas = FormatarDuracao(horas);

            var saldo = horas - TimeSpan.FromHours(JornadaEsperadaHoras);
            SaldoDia = FormatarSaldo(saldo);

            var registros = Registros.OrderBy(r => r.Horario).ToList();
            var ultimo = registros.LastOrDefault();

            if (ultimo?.Tipo == "Entrada")
                StatusJornada = "● Jornada em andamento";
            else if (ultimo?.Tipo == "Saída")
                StatusJornada = "✓ Jornada encerrada";
            else
                StatusJornada = "○ Fora da jornada";
        }

        private async Task CalcularResumoMesAsync()
        {
            var todos = await _registroService.ObterRegistrosAsync();
            var inicioMes = new DateTime(DataSelecionada.Year, DataSelecionada.Month, 1);
            var fimMes = inicioMes.AddMonths(1);

            var registrosMes = todos
                .Where(r => r.Horario >= inicioMes && r.Horario < fimMes)
                .OrderBy(r => r.Horario)
                .ToList();

            var dias = registrosMes
                .GroupBy(r => r.Horario.Date)
                .ToList();

            TimeSpan total = TimeSpan.Zero;
            var diasComJornada = 0;

            foreach (var dia in dias)
            {
                var horasDia = CalcularHorasDosRegistros(dia.OrderBy(r => r.Horario).ToList());
                if (horasDia > TimeSpan.Zero)
                {
                    total += horasDia;
                    diasComJornada++;
                }
            }

            TotalMes = FormatarDuracao(total);
            DiasTrabalhados = diasComJornada.ToString();
            MediaDiaria = diasComJornada == 0
                ? "00:00"
                : FormatarDuracao(TimeSpan.FromTicks(total.Ticks / diasComJornada));

            var esperado = TimeSpan.FromHours(JornadaEsperadaHoras * diasComJornada);
            SaldoMes = FormatarSaldo(total - esperado);
        }

        private static TimeSpan CalcularHorasDosRegistros(List<Models.RegistroPonto> registros)
        {
            TimeSpan total = TimeSpan.Zero;
            DateTime? entrada = null;

            foreach (var registro in registros)
            {
                if (registro.Tipo == "Entrada")
                    entrada = registro.Horario;
                else if (registro.Tipo == "Saída" && entrada.HasValue)
                {
                    if (registro.Horario >= entrada.Value)
                        total += registro.Horario - entrada.Value;
                    entrada = null;
                }
            }

            return total;
        }

        private async Task RegistrarEntradaAsync()
        {
            try
            {
                if (DataSelecionada.Date != DateTime.Today)
                {
                    await App.Current.MainPage.DisplayAlert("Data selecionada", "Para registrar o ponto, selecione Hoje.", "OK");
                    return;
                }

                await _registroService.AdicionarRegistroAsync("Entrada");
                await AtualizarPainelAsync();
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
                if (DataSelecionada.Date != DateTime.Today)
                {
                    await App.Current.MainPage.DisplayAlert("Data selecionada", "Para registrar o ponto, selecione Hoje.", "OK");
                    return;
                }

                await _registroService.AdicionarRegistroAsync("Saída");
                await AtualizarPainelAsync();
            }
            catch (InvalidOperationException ex)
            {
                await App.Current.MainPage.DisplayAlert("Aviso", ex.Message, "OK");
            }
        }

        private async Task EditarRegistroAsync(Models.RegistroPonto registro)
        {
            if (registro == null)
                return;

            var texto = await App.Current.MainPage.DisplayPromptAsync(
                "Corrigir horário",
                $"{registro.Tipo} registrada às {registro.Horario:HH:mm:ss}. Informe o novo horário (HH:mm ou HH:mm:ss):",
                "Salvar",
                "Cancelar",
                "Ex.: 08:00",
                maxLength: 8,
                keyboard: Keyboard.Numeric,
                initialValue: registro.Horario.ToString("HH:mm:ss"));

            if (string.IsNullOrWhiteSpace(texto))
                return;

            if (!TimeSpan.TryParseExact(texto, new[] { @"hh\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out var horario))
            {
                await App.Current.MainPage.DisplayAlert("Horário inválido", "Use o formato HH:mm ou HH:mm:ss.", "OK");
                return;
            }

            var novoHorario = registro.Horario.Date.Add(horario);
            if (await _registroService.EditarHorarioAsync(registro, novoHorario))
                await AtualizarPainelAsync();
        }

        private async Task ExcluirRegistroAsync(Models.RegistroPonto registro)
        {
            if (registro == null)
                return;

            var confirmar = await App.Current.MainPage.DisplayAlert(
                "Excluir registro",
                $"Deseja excluir {registro.Tipo.ToLowerInvariant()} das {registro.Horario:HH:mm:ss}?",
                "Excluir",
                "Cancelar");

            if (confirmar && await _registroService.ExcluirRegistroAsync(registro))
                await AtualizarPainelAsync();
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

        private static string FormatarDuracao(TimeSpan valor)
        {
            var horas = (int)valor.TotalHours;
            return $"{horas:00}:{valor.Minutes:00}";
        }

        private static string FormatarSaldo(TimeSpan valor)
        {
            var sinal = valor < TimeSpan.Zero ? "-" : "+";
            var absoluto = valor.Duration();
            var horas = (int)absoluto.TotalHours;
            return $"{sinal}{horas:00}:{absoluto.Minutes:00}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}