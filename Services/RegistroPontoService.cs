using RegistroPonto.Data;
using SQLite;
using System.Text;

namespace RegistroPonto.Services
{
    public class RegistroService
    {
        private readonly DatabaseContext _database;
        private readonly SQLiteAsyncConnection _connection;

        public RegistroService(string dbPath)
        {
            _database = new DatabaseContext(dbPath);
            _connection = new SQLiteAsyncConnection(dbPath);
        }

        public async Task AdicionarRegistroAsync(string tipo)
        {
            var registros = await _database.ObterRegistrosAsync();
            var ultimoRegistro = registros.OrderBy(r => r.Horario).LastOrDefault();

            if (tipo == "Saída" && (ultimoRegistro == null || ultimoRegistro.Tipo == "Saída"))
                throw new InvalidOperationException("Não é possível registrar uma saída sem uma entrada anterior.");

            if (tipo == "Entrada" && ultimoRegistro?.Tipo == "Entrada")
                throw new InvalidOperationException("Já existe uma entrada sem saída. Registre a saída antes de iniciar outra jornada.");

            var registro = new Models.RegistroPonto
            {
                Tipo = tipo,
                Horario = DateTime.Now
            };

            await _database.SalvarRegistroAsync(registro);
        }

        public async Task<List<Models.RegistroPonto>> ObterRegistrosAsync()
        {
            return (await _database.ObterRegistrosAsync())
                .OrderBy(r => r.Horario)
                .ToList();
        }

        public async Task<List<Models.RegistroPonto>> ObterRegistrosPorDataAsync(DateTime data)
        {
            var inicio = data.Date;
            var fim = inicio.AddDays(1);

            return (await _database.ObterRegistrosAsync())
                .Where(r => r.Horario >= inicio && r.Horario < fim)
                .OrderByDescending(r => r.Horario)
                .ToList();
        }

        public async Task<TimeSpan> CalcularHorasTrabalhadasAsync(DateTime? data = null)
        {
            var registros = data.HasValue
                ? await ObterRegistrosPorDataAsync(data.Value)
                : await ObterRegistrosAsync();

            var ordenados = registros.OrderBy(r => r.Horario).ToList();
            TimeSpan horasTrabalhadas = TimeSpan.Zero;
            DateTime? entradaAnterior = null;

            foreach (var registro in ordenados)
            {
                if (registro.Tipo == "Entrada")
                    entradaAnterior = registro.Horario;
                else if (registro.Tipo == "Saída" && entradaAnterior.HasValue)
                {
                    if (registro.Horario >= entradaAnterior.Value)
                        horasTrabalhadas += registro.Horario - entradaAnterior.Value;

                    entradaAnterior = null;
                }
            }

            return horasTrabalhadas;
        }

        public async Task<bool> EditarHorarioAsync(Models.RegistroPonto registro, DateTime novoHorario)
        {
            if (novoHorario == registro.Horario)
                return false;

            var afetados = await _connection.ExecuteAsync(
                "UPDATE RegistroPonto SET Horario = ? WHERE Tipo = ? AND Horario = ?",
                novoHorario,
                registro.Tipo,
                registro.Horario);

            return afetados > 0;
        }

        public async Task<bool> ExcluirRegistroAsync(Models.RegistroPonto registro)
        {
            var afetados = await _connection.ExecuteAsync(
                "DELETE FROM RegistroPonto WHERE Tipo = ? AND Horario = ?",
                registro.Tipo,
                registro.Horario);

            return afetados > 0;
        }

        public async Task<string> ExportarParaCsvAsync()
        {
            var registros = await ObterRegistrosAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Tipo,Horário");

            foreach (var registro in registros)
                csv.AppendLine($"{registro.Tipo},{registro.Horario:yyyy-MM-dd HH:mm:ss}");

            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "registros.csv");

            await File.WriteAllTextAsync(filePath, csv.ToString());
            return filePath;
        }
    }
}