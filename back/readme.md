# Beach tennis booking API

The backend is split into four projects:

- `Domain`: entities, enums, and domain invariants.
- `Application`: booking use cases, pricing, and persistence contracts.
- `Infrastructure`: EF Core/SQLite persistence.
- `BeachAula4.csproj`: HTTP endpoints, configuration, and API error handling.

## Run locally

From this directory:

```powershell
dotnet run --project .\BeachAula4.csproj
```

The API applies pending EF Core migrations at startup. The current SQLite database and court rows are preserved. The connection string is in `appsettings.json`; use environment-specific configuration or user secrets for deployment.

## Initial business settings

`appsettings.json` provides example values intended for local development:

- R$ 100 per hour;
- 60-minute minimum booking, up to 180 minutes;
- booking start times in 60-minute increments;
- 20% discount for associates and 30% for instructors;
- daily opening hours from 08:00 to 22:00;
- club timezone configured as `E. South America Standard Time`.

Edit `Reservas` and `HorarioFuncionamento` for the club's policies. To close a day, remove it from `HorarioFuncionamento:Dias`. The enum values for `TipoCliente` are `Avulso`, `Associado`, and `Professor`.

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/clientes` | Register a customer |
| GET | `/api/clientes/{id}` | Get a customer |
| POST | `/api/quadras` | Register a court |
| GET | `/api/quadras` | List courts |
| GET | `/api/quadras/{quadraId}/disponibilidade?data=2026-10-05&duracaoMinutos=60&tipoCliente=Associado` | List time slots and prices |
| POST | `/api/reservas` | Create and immediately confirm a booking |
| GET | `/api/reservas/{id}` | Get a booking |
| GET | `/api/reservas?clienteId=1` | List a customer's bookings |
| POST | `/api/reservas/{id}/cancelamento` | Cancel a future confirmed booking |

Request examples:

```json
{
  "name": "Maria Silva",
  "email": "maria@example.com",
  "phone": "+55 11 99999-9999",
  "tipo": "Associado"
}
```

```json
{
  "clienteId": 1,
  "quadraId": 1,
  "inicio": "2026-10-05T10:00:00-03:00",
  "fim": "2026-10-05T11:00:00-03:00"
}
```

Bookings are confirmed as soon as they are created; there is no payment or pending-payment state. A confirmed court/time slot is blocked until the reservation is cancelled. Cancellation is allowed only before the reservation starts. Same-day availability omits time slots that have already passed. A successful create response (`201 Created`) is the confirmation for this proof of concept; it does not send email or SMS. Responses use `ProblemDetails`; invalid business rules return 422, missing resources 404, and duplicate bookings 409.

## Integration boundary

This is a proof of concept: creating a reservation immediately confirms it and the API returns the confirmation. There is no payment, email, or SMS functionality. Before production, add authentication/authorization, an actual confirmation channel if required, and automated tests against the production database provider.
