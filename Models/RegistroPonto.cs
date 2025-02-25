namespace RegistroPonto.Models
{
    public class RegistroPonto
    {
        public string Tipo { get; set; } // "Entrada" ou "Saída"
        public DateTime Horario { get; set; }

        public override string ToString()
        {
            return $"{Tipo}: {Horario.ToString("HH:mm:ss")}";
        }
    }
}