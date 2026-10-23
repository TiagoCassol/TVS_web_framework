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

## Validate inputs and run tests

Customer and court names are trimmed and repeated whitespace is collapsed. Customer names accept accented letters, spaces, hyphens, and apostrophes; court names additionally accept digits. CPF and phone accept only their usual formatting punctuation and are stored as digits. CPF check digits, phone length, and optional e-mail format are validated; unexpected characters are rejected instead of silently removed. Reservation requests must provide positive customer/court IDs and `tipoCliente`; values outside `Padrao=0`, `Socio=1`, and `Aluno=2` are rejected.

Run the automated API tests from this directory:

```powershell
dotnet test .\BeachAula4.slnx
```

The tests use a temporary SQLite database and do not modify the development database.

## Initial business settings

`appsettings.json` provides example values intended for local development:

- R$ 100 per hour;
- booking durations of 60, 90, or 120 minutes;
- booking start times in 30-minute increments;
- 20% discount for `Socio` and 30% for `Aluno`;
- daily opening hours from 07:00 to 22:00;
- club timezone configured as `E. South America Standard Time`.

Edit `Reservas` and `HorarioFuncionamento` for the club's policies. To close a day, remove it from `HorarioFuncionamento:Dias`. The front-end booking type values are `Padrao` (0), `Socio` (1), and `Aluno` (2).

The local demonstration login is `gestor@beachtennis.local` / `demo1234`, configurable in `Autenticacao`. This endpoint only checks credentials and returns a user object; it does not issue a token or protect management routes. Do not use it as production authentication.

## Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| POST | `/api/auth/login` | Validate `{ email, senha }` and return `{ id, nome, email, perfil }` |
| POST | `/api/clientes` | Register a customer |
| GET | `/api/clientes/{id}` | Get a customer |
| GET | `/api/clientes` | List customers |
| GET | `/api/clientes?cpf=00000000000` | Find a customer by CPF (empty array if not found) |
| POST | `/api/quadras` | Register a court using `{ nome }` |
| GET | `/api/quadras` | List courts |
| GET | `/api/quadras/{quadraId}/disponibilidade?data=2026-10-23` | List time slots |
| POST | `/api/reservas` | Create and immediately confirm a booking |
| GET | `/api/reservas/{id}` | Get a booking |
| GET | `/api/reservas?data=2026-10-23&clienteId=1` | List bookings for a day, optionally filtered by customer |
| POST | `/api/reservas/{id}/cancelamento` | Cancel a future confirmed booking |

Request examples:

```json
{
  "nome": "Maria Silva",
  "cpf": "52998224725",
  "telefone": "51999990001",
  "email": "maria@example.com"
}
```

```json
{
  "clienteId": 1,
  "quadraId": 1,
  "tipoCliente": 0,
  "inicio": "2026-10-05T10:00:00-03:00",
  "fim": "2026-10-05T11:00:00-03:00"
}
```

Bookings are confirmed as soon as they are created; there is no payment or pending-payment state. A confirmed court/time slot is blocked until the reservation is cancelled. Cancellation is allowed only before the reservation starts. Same-day availability omits time slots that have already passed. A successful create response (`201 Created`) is the confirmation for this proof of concept; it does not send email or SMS. Responses use `ProblemDetails`; invalid business rules return 422, missing resources 404, and duplicate bookings 409.

Customer CPF must be valid and unique. Phone must contain 10 or 11 digits; e-mail is optional. The API accepts formatted or digits-only phone/CPF, and returns only digits. Legacy customers without CPF remain in the database and are returned with an empty CPF until updated.

`GET /api/reservas` requires at least one filter (`data` and/or `clienteId`). The time-slot duration defaults to 60 minutes and can be requested with `duracaoMinutos=60`, `90`, or `120`. The booking request must include `tipoCliente` using the front-end numeric values: `Padrao=0`, `Socio=1`, `Aluno=2`.

## Integration boundary

This is a proof of concept: creating a reservation immediately confirms it and the API returns the confirmation. There is no payment, email, or SMS functionality. Before production, add authentication/authorization, an actual confirmation channel if required, and automated tests against the production database provider.
