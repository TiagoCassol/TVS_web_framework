# Avaliação e proposta de arquitetura

## Contexto

Este documento avalia o backend inicial do sistema de reservas de um clube de beach tennis e propõe uma evolução baseada em Clean Architecture.

O objetivo do sistema é suportar:

- validação dos dados;
- consulta de disponibilidade;
- verificação do horário de funcionamento;
- prevenção de conflitos entre reservas;
- cálculo de valor;
- aplicação de descontos;
- registro de clientes;
- gravação de reservas;
- registro de pagamentos;
- envio de confirmação.

## Situação atual

O backend atual é uma Minimal API em um único projeto, com entidades, DTOs, contexto EF Core e endpoints concentrados no `Program.cs`.

Principais limitações:

1. A aplicação não está dividida em camadas.
2. A lógica de negócio está misturada com a camada HTTP.
3. A validação do intervalo de datas está invertida no endpoint de reservas.
4. O endpoint cria uma reserva, mas não a persiste.
5. O valor calculado não é atribuído à reserva.
6. A migração atual cria somente a tabela `Quadras`.
7. Não existem tabelas para clientes, reservas ou pagamentos.
8. O DTO de criação de cliente contém propriedades de reserva.
9. Não há verificação de existência de cliente ou quadra.
10. Não há validação de horário de funcionamento.
11. Não há verificação de conflito com outras reservas.
12. O pagamento não está relacionado a uma reserva.
13. Não há fluxo de confirmação ou tratamento de notificações.
14. O SQLite está configurado diretamente no código.
15. Não há testes automatizados.

## Estrutura recomendada

```text
src/
├── BeachTennis.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── ValueObjects/
│   ├── Errors/
│   └── Services/
├── BeachTennis.Application/
│   ├── UseCases/
│   ├── Interfaces/
│   └── Validators/
├── BeachTennis.Infrastructure/
│   ├── Persistence/
│   ├── Repositories/
│   ├── Payments/
│   └── Notifications/
└── BeachTennis.Api/
    ├── Endpoints/
    ├── Contracts/
    ├── Middleware/
    └── Program.cs
```

Dependências:

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
Api
```

A camada `Domain` não deve depender de ASP.NET, Entity Framework Core ou serviços externos.

## Modelo de domínio

### Cliente

Deve conter, no mínimo:

- `Id`;
- `Nome`;
- `Email`;
- `Telefone`;
- `Ativo`.

Regras:

- nome obrigatório;
- e-mail válido e único;
- telefone válido;
- cliente inativo não pode criar reserva.

### Quadra

Deve conter:

- `Id`;
- `Nome`;
- `Ativa`.

Uma quadra inativa não pode receber novas reservas.

### Reserva

Campos recomendados:

- `Id`;
- `ClienteId`;
- `QuadraId`;
- `Inicio`;
- `Fim`;
- `ValorBruto`;
- `Desconto`;
- `ValorFinal`;
- `Status`;
- `CriadaEm`.

Usar `DateTimeOffset` para representar horários e configurar explicitamente o fuso do clube.

Status sugeridos:

```csharp
public enum StatusReserva
{
    PendentePagamento = 1,
    Confirmada = 2,
    Cancelada = 3,
    Expirada = 4,
    Concluida = 5
}
```

### Pagamento

O pagamento deve ser relacionado à reserva, e não apenas ao cliente:

- `Id`;
- `ReservaId`;
- `Valor`;
- `Status`;
- `IdExterno`;
- `PagoEm`.

Status sugeridos:

```csharp
public enum StatusPagamento
{
    Pendente = 1,
    Aprovado = 2,
    Recusado = 3,
    Estornado = 4
}
```

## Fluxo para criar uma reserva

O caso de uso `CriarReserva` deve:

1. validar o formato e as regras básicas;
2. buscar o cliente;
3. verificar se o cliente está ativo;
4. buscar a quadra;
5. verificar se a quadra está ativa;
6. verificar o horário de funcionamento;
7. validar duração mínima e máxima;
8. verificar conflito de horário;
9. calcular o preço;
10. aplicar o desconto;
11. criar a reserva como `PendentePagamento`;
12. criar o pagamento;
13. persistir os dados em uma transação;
14. processar ou iniciar o pagamento;
15. confirmar a reserva após pagamento aprovado;
16. enviar a confirmação.

O envio da confirmação deve ser assíncrono. O fluxo pode publicar eventos como:

- `ReservaCriada`;
- `PagamentoAprovado`;
- `ReservaConfirmada`.

O padrão Outbox deve ser considerado para evitar perda de mensagens.

## Regras essenciais

### Conflito de reservas

Dois intervalos entram em conflito quando:

```text
novoInicio < reservaExistenteFim
AND
novoFim > reservaExistenteInicio
```

Somente reservas ativas ou pendentes devem impedir uma nova reserva.

Também é necessário tratar concorrência para impedir que duas requisições simultâneas reservem o mesmo horário.

### Horário de funcionamento

Criar uma configuração ou entidade para suportar:

- horários diferentes por dia da semana;
- feriados;
- manutenção;
- horários especiais;
- intervalos de indisponibilidade.

### Duração

Definir explicitamente:

- duração mínima;
- duração máxima;
- intervalos permitidos, como 30, 60 ou 90 minutos;
- horários permitidos para início.

### Preço e desconto

A regra de preço deve ser isolada em um serviço, por exemplo:

```csharp
public interface ICalculadoraPrecoReserva
{
    ValorReserva Calcular(
        DateTimeOffset inicio,
        DateTimeOffset fim,
        TipoCliente tipoCliente);
}
```

Usar `decimal` para valores monetários. Evitar números mágicos como `TipoCliente == 1`; usar enums ou regras configuráveis.

## Endpoints sugeridos

```text
POST /api/clientes
GET  /api/clientes/{id}
PUT  /api/clientes/{id}

POST /api/quadras
GET  /api/quadras
GET  /api/quadras/{id}
PUT  /api/quadras/{id}

GET  /api/quadras/{quadraId}/disponibilidade?data=2026-09-25

POST /api/reservas
GET  /api/reservas/{id}
GET  /api/reservas?clienteId=10&data=2026-09-25
POST /api/reservas/{id}/cancelamento

POST /api/reservas/{reservaId}/pagamento
GET  /api/pagamentos/{id}
POST /api/pagamentos/webhook
```

## Persistência

Registrar o `DbContext` por injeção de dependência e obter a connection string da configuração:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=beachtennis.db"
  }
}
```

Adicionar configurações EF Core para:

- chaves estrangeiras;
- campos obrigatórios;
- precisão decimal;
- enums;
- unicidade de e-mail;
- índices de busca;
- relacionamentos entre reserva e pagamento.

Índices recomendados:

- `Clientes.Email UNIQUE`;
- `Reservas.QuadraId + Reservas.Inicio`;
- `Reservas.ClienteId + Reservas.Inicio`;
- `Pagamentos.ReservaId`.

SQLite é adequado para desenvolvimento. Para produção, PostgreSQL ou SQL Server devem ser avaliados, especialmente por concorrência e operação multiusuário.

## Tratamento de erros

Usar `ProblemDetails` com respostas padronizadas:

- `400 Bad Request`: dados inválidos;
- `404 Not Found`: entidade inexistente;
- `409 Conflict`: horário indisponível;
- `422 Unprocessable Entity`: regra de negócio não atendida;
- `500 Internal Server Error`: erro inesperado.

## Plano de implementação

### Fase 1 — Modelo e persistência

1. Separar Domain, Application, Infrastructure e Api.
2. Corrigir construtores e métodos das entidades.
3. Corrigir o DTO de criação de cliente.
4. Adicionar `DbSet` de clientes, reservas e pagamentos.
5. Criar migrações atualizadas.
6. Corrigir a validação de datas.
7. Implementar cadastro de clientes.

### Fase 2 — Reservas

1. Criar o comando e handler `CriarReserva`.
2. Validar cliente e quadra.
3. Validar horário de funcionamento.
4. Verificar conflitos.
5. Calcular preço e desconto.
6. Persistir reserva e pagamento em transação.
7. Implementar consulta de disponibilidade.

### Fase 3 — Pagamentos

1. Criar a abstração `IPaymentGateway`.
2. Implementar um gateway fake para desenvolvimento.
3. Adicionar status de pagamento.
4. Implementar webhook.
5. Confirmar a reserva somente após aprovação.

### Fase 4 — Confirmações

1. Criar eventos de domínio.
2. Implementar Outbox.
3. Criar worker de notificações.
4. Implementar e-mail.
5. Adicionar WhatsApp ou SMS posteriormente.

### Fase 5 — Testes

Cobrir pelo menos:

- intervalo inválido;
- duração menor que o mínimo;
- reserva fora do funcionamento;
- quadra ou cliente inexistente;
- conflito parcial;
- conflito completo;
- desconto;
- cancelamento;
- pagamento recusado;
- tentativas simultâneas de reserva.

## Prioridades imediatas

1. Corrigir os erros de compilação em `Reserva.cs`.
2. Corrigir a validação invertida em `Program.cs`.
3. Persistir reservas.
4. Adicionar as tabelas de clientes, reservas e pagamentos.
5. Implementar horários de funcionamento.
6. Implementar verificação de conflito.
7. Remover regras de negócio do `Program.cs`.
8. Corrigir o DTO de cliente.
9. Relacionar pagamento diretamente à reserva.
10. Criar testes automatizados antes de integrar um gateway real.

