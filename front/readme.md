# Frontend — Gestão de Quadras de Beach Tennis

Vue 3 + TypeScript + Vite. SPA independente da Minimal API: comunica-se **apenas por HTTP/JSON**
e pode rodar sem o backend (modo mock).

## Áreas do site

| Rota                | Quem usa | O que faz                                                                  |
| ------------------- | -------- | -------------------------------------------------------------------------- |
| `/`                 | Cliente  | Vê as quadras e os horários livres dos próximos 7 dias e reserva sozinho |
| `/entrar`           | Gestor   | Login com e-mail e senha                                                   |
| `/gestao/reservas`  | Gestor   | Agenda por quadra, cria reservas para qualquer cliente, cancela            |
| `/gestao/quadras`   | Gestor   | Cadastro de quadras                                                        |
| `/gestao/clientes`  | Gestor   | Cadastro e consulta de clientes                                            |

**Reserva pelo cliente:** ele escolhe o dia e toca em um horário livre. O site só oferece as
durações que cabem antes do próximo horário ocupado e esconde horários que já passaram. Depois
ele informa os **dados mínimos (nome, CPF e telefone; e-mail opcional)**. O caso de uso
`useReservaPublica` procura o cliente pelo CPF; se não houver cadastro, cria um; em seguida cria a
reserva.

### Login do gestor

As rotas `/gestao/*` exigem login. Quem tenta acessá-las sem estar logado vai para
`/entrar?voltar=<rota>` e, depois de entrar, volta para onde queria ir. O login usa e-mail e senha:

- A porta `AuthGateway` (`application/ports.ts`) define `entrar`, `sair` e `usuarioAtual`.
- `application/sessao.ts` guarda o usuário logado de forma reativa e é usada pelo guarda de rotas e pelo
  cabeçalho.
- A sessão fica no `sessionStorage` da aba (`infrastructure/sessaoArmazenada.ts`) e sobrevive a um F5.
  Ali ficam só id, nome, e-mail e perfil, **nunca a senha**.
- **Modo mock:** o acesso de demonstração está em `infrastructure/mock/mockAuth.ts` (`GESTOR_DEMO`). A
  tela de login tem o botão "Usar acesso de teste", que só aparece nesse modo.
- **Modo http:** chama `POST /api/auth/login` (veja o contrato abaixo).

> ⚠️ **Por enquanto, isto é só visual.** Sem token, a API não sabe quem está chamando, então os endpoints
> de gestão continuam abertos para quem chamar a API diretamente. O próximo passo é o backend emitir um
> JWT no login e exigir `[Authorize(Roles = "Gestor")]` nos endpoints de gestão. No front, só mudam o
> adaptador de auth (guardar o token) e o `httpClient` (enviar `Authorization: Bearer`).

## Como rodar

```bash
npm install
npm run dev       # modo mock: dados em memória, não precisa do backend
npm run dev:api   # modo http: chama a API em http://localhost:5289 via proxy do Vite
npm test          # testes de domínio, mock e contratos
npm run build     # checagem de tipos + build de produção (pasta dist/)
```

A fonte de dados é escolhida em `.env` / `.env.api`:

| Variável            | Valores         | Uso                                                  |
| ------------------- | --------------- | ---------------------------------------------------- |
| `VITE_API_MODE`     | `mock` / `http` | Qual adaptador implementa as portas                  |
| `VITE_API_URL`      | URL ou vazio    | Base da API. Vazio = mesma origem (proxy em dev)     |
| `VITE_PROXY_TARGET` | URL             | Para onde o proxy do Vite repassa `/api` em dev      |

## Arquitetura

O front segue a mesma ideia de camadas do backend. As dependências apontam só para dentro:

```text
presentation  ──►  application  ──►  domain
                        ▲
infrastructure ─────────┘   (implementa as portas da application)
```

```text
src/
├── domain/            TypeScript puro: tipos e regras (conflito, duração, validação)
├── application/
│   ├── ports.ts       Interfaces QuadraGateway, ClienteGateway, ReservaGateway
│   ├── erros.ts       AppError: erro único que as telas conhecem
│   ├── injecao.ts     Chave de injeção de dependência (provide/inject)
│   ├── useReservaPublica.ts  Caso de uso do cliente: agenda do dia + reservar com CPF
│   └── use*.ts        Casos de uso da gestão (estado + chamadas às portas)
├── infrastructure/
│   ├── http/          Adaptador real: fetch, DTOs, mapeadores, ProblemDetails → AppError
│   ├── mock/          Adaptador em memória que simula as regras do backend
│   └── container.ts   Composition root: escolhe mock ou http
└── presentation/
    ├── layouts/       LayoutPublico (cliente) e LayoutGestao (gestor)
    ├── views/publico/ Agenda e reserva do cliente
    ├── views/gestao/  Reservas, quadras e clientes
    └── components/    Peças reaproveitadas pelas duas áreas (grade de horários, alertas)
```

### Por que é desacoplado

- **Telas não conhecem HTTP.** Componentes chamam `useReservas()`, que fala com a interface
  `ReservaGateway`. Não há `fetch` nem URL fora de `infrastructure/http`.
- **O formato do JSON fica isolado.** `infrastructure/http/contratos.ts` é o único arquivo que
  conhece os DTOs da API. Se o backend renomear `Name` para `Nome` ou trocar `Status` de número para
  texto, só os mapeadores mudam. Hoje eles aceitam os dois formatos.
- **Sem números mágicos.** O domínio usa `TipoCliente = 'Padrao' | 'Socio' | 'Aluno'`; o mapeador
  converte para o inteiro esperado pela API (0, 1, 2).
- **Erros padronizados.** Status 400/404/409/422 e `ProblemDetails` viram `AppError` com um `tipo`;
  a tela só exibe a mensagem.
- **Desenvolvimento em paralelo.** Com o adaptador mock, front e back evoluem de forma independente;
  os dois só precisam concordar com o contrato abaixo.
- **Deploy separado.** O `dist/` é estático e pode ser servido por qualquer servidor/CDN.

## Contrato esperado da API

Baseado em `ARCHITECTURE_REVIEW.md`. Campos em camelCase (padrão do System.Text.Json).

| Método | Rota                                              | Corpo / resposta                                          |
| ------ | ------------------------------------------------- | --------------------------------------------------------- |
| POST   | `/api/auth/login`                                 | `{ email, senha }` → `{ id, nome, email, perfil }` ou 401 |
| GET    | `/api/quadras`                                    | `[{ id, nome, ativa }]`                                   |
| POST   | `/api/quadras`                                    | `{ nome }` → quadra                                       |
| GET    | `/api/clientes`                                   | `[{ id, nome, cpf, telefone, email, ativo }]`             |
| GET    | `/api/clientes?cpf=00000000000`                   | `[cliente]` (vazio se não existir)                        |
| POST   | `/api/clientes`                                   | `{ nome, cpf, telefone, email? }` → cliente               |
| GET    | `/api/quadras/{id}/disponibilidade?data=AAAA-MM-DD` | `[{ inicio, fim, disponivel }]`                         |
| GET    | `/api/reservas?data=AAAA-MM-DD&clienteId=`        | `[reserva]`                                               |
| POST   | `/api/reservas`                                   | `{ clienteId, quadraId, tipoCliente, inicio, fim }` → reserva |
| POST   | `/api/reservas/{id}/cancelamento`                 | —                                                         |

Reserva: `{ id, clienteId, quadraId, inicio, fim, valorBruto, desconto, valorFinal, status }`,
com `status` de 1 a 5 (`PendentePagamento`, `Confirmada`, `Cancelada`, `Expirada`, `Concluida`)
ou o nome do enum. Datas em ISO 8601.

Erros: `ProblemDetails` com `400`, `404`, `409` (horário indisponível) ou `422` (regra de negócio).

`GET /api/clientes` e o filtro `?cpf=` não constam na análise de arquitetura, mas o front precisa
deles: a gestão lista clientes e a área pública reaproveita o cadastro pelo CPF.

CPF e telefone são enviados **só com dígitos**. O backend deve validar o CPF (dígitos verificadores),
garantir **CPF único** e tratar e-mail como opcional.

Hoje a reserva do cliente faz até três chamadas (buscar por CPF, cadastrar, reservar). Uma melhoria
para o backend seria um único `POST /api/reservas` que aceite os dados do cliente e faça tudo numa
transação. Só `useReservaPublica` e o adaptador HTTP mudariam.

### Dados pessoais (LGPD)

CPF e telefone são dados pessoais. O front os pede só no momento da reserva e informa o motivo.
A agenda pública (`/disponibilidade`) **não pode** devolver nome, CPF ou telefone de quem reservou,
apenas se o horário está livre.

### CORS em produção

Em desenvolvimento o proxy do Vite evita CORS. Se o front for publicado em outra origem,
a API precisa liberar essa origem:

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
// ...
app.UseCors();
```

## Regras simuladas no modo mock

Funcionamento das 7h às 22h, duração mínima de 60 min, R$ 100/hora, desconto de 20% para
sócio e 30% para aluno (mesmos percentuais do `Program.cs`), bloqueio de conflito de horário.
Elas existem só para a demonstração: **a fonte da verdade é o backend.**
