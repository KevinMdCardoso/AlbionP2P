# Albion P2P — Solution unificada (API + Web)

Marketplace P2P para o jogo Albion Online. Uma única solution com todos os projetos — back-end e front-end na mesma IDE.

## Estrutura

```
AlbionP2P.sln
├── src/
│   ├── AlbionP2P.Domain          → Aggregates, Value Objects, Domain Events, interfaces
│   ├── AlbionP2P.Application     → Handlers CQRS, DTOs
│   ├── AlbionP2P.Infrastructure  → EF Core, SQL Server, Repositórios
│   ├── AlbionP2P.API             → ASP.NET Core Web API + SignalR
│   │   └── roda em https://localhost:7001
│   └── AlbionP2P.Web             → Blazor WebAssembly
│       └── roda em https://localhost:7002
└── tests/
    ├── AlbionP2P.Domain.Tests
    └── AlbionP2P.Application.Tests
```

## Stack

| Camada       | Tecnologia                            |
|--------------|---------------------------------------|
| Back-end     | .NET 8 / ASP.NET Core Web API         |
| Arquitetura  | DDD + CQRS (handlers manuais)         |
| Auth         | ASP.NET Core Identity (cookie, SameSite=Lax) |
| ORM          | Entity Framework Core 8               |
| Banco        | SQL Server                            |
| Tempo real   | SignalR                               |
| Front-end    | Blazor WebAssembly (.NET 8)           |
| Testes       | xUnit + FluentAssertions + NSubstitute|

## Telas (Blazor)

| Rota           | Tela                       | Auth |
|----------------|----------------------------|------|
| `/`            | Feed de pedidos            | —    |
| `/login`       | Login                      | —    |
| `/register`    | Registro                   | —    |
| `/dashboard`   | Meus pedidos               | ✓    |
| `/orders/new`  | Criar pedido               | ✓    |
| `/deals`       | Lista de negociações       | ✓    |
| `/deals/{id}`  | Chat + ações do deal       | ✓    |

---

## Como rodar

### Pré-requisitos
- .NET 8 SDK
- SQL Server (local ou Docker)

### 1. SQL Server via Docker (opcional)
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=AlbionP2P@123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```
Nesse caso, use a connection string:
```
Server=localhost,1433;Database=AlbionP2P_Dev;User Id=sa;Password=AlbionP2P@123;TrustServerCertificate=True;
```

### 2. Ajustar a connection string
Edite `src/AlbionP2P.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AlbionP2P_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Criar o banco de dados (migrations)
```bash
dotnet ef migrations add InitialCreate \
  --project src/AlbionP2P.Infrastructure \
  --startup-project src/AlbionP2P.API

dotnet ef database update \
  --project src/AlbionP2P.Infrastructure \
  --startup-project src/AlbionP2P.API
```
> O `EnsureCreated()` no `Program.cs` já cria o schema automaticamente em desenvolvimento sem precisar de migrations. Remova para produção.

### 4. Rodar os dois projetos juntos (Visual Studio)

**Opção A — Visual Studio (recomendado):**
1. Clique com botão direito na solution → `Properties`
2. `Common Properties` → `Startup Project` → `Multiple startup projects`
3. Marque `AlbionP2P.API` e `AlbionP2P.Web` como **Start**
4. Pressione `F5`

**Opção B — dois terminais:**
```bash
# Terminal 1
dotnet run --project src/AlbionP2P.API

# Terminal 2
dotnet run --project src/AlbionP2P.Web
```

| Projeto         | URL                          |
|-----------------|------------------------------|
| API + Swagger   | https://localhost:7001/swagger |
| Blazor Web      | https://localhost:7002       |

### 5. Rodar os testes
```bash
dotnet test
```

---

## Endpoints da API

### Auth
| Método | Rota                    | Auth | Descrição        |
|--------|-------------------------|------|------------------|
| POST   | /api/account/register   | —    | Criar conta      |
| POST   | /api/account/login      | —    | Login            |
| POST   | /api/account/logout     | ✓    | Logout           |
| GET    | /api/account/me         | ✓    | Usuário logado   |

### Orders
| Método | Rota                    | Auth | Descrição             |
|--------|-------------------------|------|-----------------------|
| GET    | /api/orders             | —    | Feed (com filtros)    |
| GET    | /api/orders/mine        | ✓    | Meus pedidos          |
| POST   | /api/orders             | ✓    | Criar pedido          |
| DELETE | /api/orders/{id}        | ✓    | Cancelar pedido       |

### Deals
| Método | Rota                        | Auth | Descrição              |
|--------|-----------------------------|------|------------------------|
| GET    | /api/deals/mine             | ✓    | Minhas negociações     |
| GET    | /api/deals/{id}             | ✓    | Detalhe do deal        |
| POST   | /api/deals                  | ✓    | Criar negociação       |
| POST   | /api/deals/{id}/accept      | ✓    | Aceitar proposta       |
| POST   | /api/deals/{id}/reject      | ✓    | Rejeitar proposta      |
| POST   | /api/deals/{id}/complete    | ✓    | Confirmar trade        |
| GET    | /api/deals/{id}/messages    | ✓    | Mensagens do chat      |

### Chat (SignalR)
- **Hub:** `/hubs/chat`
- **Métodos:** `JoinDeal(dealId)`, `LeaveDeal(dealId)`, `SendMessage(dealId, content)`
- **Eventos:** `ReceiveMessage(MessageDto)`, `JoinedDeal(dealId)`, `Error(message)`

---

## Próximos passos sugeridos
- [ ] Scroll automático no chat (JSInterop)
- [ ] Notificações toast para mensagens recebidas em segundo plano
- [ ] Página de perfil do usuário
- [ ] App Mobile futura (MAUI/Flutter) — API já está pronta para ser consumida
