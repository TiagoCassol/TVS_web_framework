namespace BeachAula4.Entidades
{
    public class Reserva
    {
        public int Id { get; set; } = 0;
        public int QuadraId { get; set; } = 0;
        public int ClienteId { get; set; } = 0;
        public DateTime Inicio {  get; set; } = DateTime.Now;
        public DateTime Fim {  get; set; } = DateTime.Now;
        public int Status { get; set; } = 0;
        public decimal Valor { get; set; } = 0;

        public Reserva(int clienteId, int quadraId, DateTime inicio, DateTime fim)
        {
            if(fim <= inicio)
            {
                Status = 1;
                return;
            }
            var timeSpan = fim - inicio;
            if(timeSpan < TimeSpan.FromMinutes(60))
            {
                Status = 2;
                return;
            }
            QuadraId = quadraId;
            ClienteId = clienteId;
            Inicio = inicio;
            Fim = fim;
            Valor = 50;
        }
    }
}
