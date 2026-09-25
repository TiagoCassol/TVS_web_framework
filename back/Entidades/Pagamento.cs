public class Pagamento
{
  public int Id { get; set; }
  public int ClienteId { get; set; }
  public decimal Valor { get; set; }
  public DateTime DataPagamento { get; set; }

  public RegistrarPagamento(int clienteId, decimal valor)
  {
    ClienteId = clienteId;
    Valor = valor;
    DataPagamento = DateTime.Now;
  }
}