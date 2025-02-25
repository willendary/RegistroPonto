using RegistroPonto.Data;
using System.Text;

namespace RegistroPonto.Services
{
    public class RegistroService
    {
        private readonly DatabaseContext _database;

        public RegistroService(string dbPath)
        {
            _database = new DatabaseContext(dbPath);
        }

        public async Task AdicionarRegistroAsync(string tipo)
        {
            var registros = await _database.ObterRegistrosAsync();
            if (registros.Count < 6)
            {
                var ultimoRegistro = registros.LastOrDefault();
                if (tipo == "Saída" && (ultimoRegistro == null || ultimoRegistro.Tipo == "Saída"))
                {
                    throw new InvalidOperationException("Não é possível registrar uma saída sem uma entrada anterior.");
                }

                var registro = new Models.RegistroPonto
                {
                    Tipo = tipo,
                    Horario = DateTime.Now
                };
                await _database.SalvarRegistroAsync(registro);
            }
            else
            {
                throw new InvalidOperationException("Limite de 6 registros atingido.");
            }
        }

        public async Task<List<Models.RegistroPonto>> ObterRegistrosAsync()
        {
            return await _database.ObterRegistrosAsync();
        }

        public async Task<TimeSpan> CalcularHorasTrabalhadasAsync()
        {
            var registros = await _database.ObterRegistrosAsync();
            TimeSpan horasTrabalhadas = TimeSpan.Zero;
            DateTime? entradaAnterior = null;

            foreach (var registro in registros)
            {
                if (registro.Tipo == "Entrada")
                {
                    entradaAnterior = registro.Horario;
                }
                else if (registro.Tipo == "Saída" && entradaAnterior.HasValue)
                {
                    horasTrabalhadas += registro.Horario - entradaAnterior.Value;
                    entradaAnterior = null;
                }
            }

            return horasTrabalhadas;
        }

        public async Task<string> ExportarParaCsvAsync()
        {
            var registros = await _database.ObterRegistrosAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Tipo,Horário");

            foreach (var registro in registros)
            {
                csv.AppendLine($"{registro.Tipo},{registro.Horario:yyyy-MM-dd HH:mm:ss}");
            }

            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "registros.csv");
            await File.WriteAllTextAsync(filePath, csv.ToString());

            return filePath;
        }
    }
}