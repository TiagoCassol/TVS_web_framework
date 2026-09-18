namespace BeachAula4.Dtos
{
    public class CriarReservaDto
    {
        public int ClienteId { get; set; }
        public int TipoCliente { get; set; }
        public int QuadraId { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fim {  get; set; }
    }
}
