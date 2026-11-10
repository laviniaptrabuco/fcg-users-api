# FCG Users API (Fase 4)

Microsserviço de usuários, autenticação (JWT) e perfis da FIAP Cloud Games.
Guia central de infraestrutura, pipeline e demonstração: **fcg-orchestration** (README).

## Persistência poliglota

| Dado | Banco | Por quê |
|---|---|---|
| Usuários, credenciais, papéis | **SQL Server** | dados centrais, relacionais e transacionais |
| Perfis de usuário (bio, gêneros favoritos…) | **MongoDB** (`MongoDB.Driver`) | documento flexível, esquema livre |
| Cache de perfis (TTL 30 min) | **Redis** (`IDistributedCache`/StackExchange.Redis) | evita round-trip ao MongoDB (resposta traz `source: mongodb\|redis`) |

## Endpoints

| Método | Rota | Auth |
|---|---|---|
| POST | `/api/auth/register` · `/api/auth/login` | Não |
| GET/PUT/DELETE | `/api/users`, `/api/users/{id}` | JWT (Admin p/ listar) |
| GET/PUT | `/api/profiles/{userId}` | JWT |
| GET | `/health` · `/metrics` | Não |

## Configuração (variáveis de ambiente — nada fixo no código)

`ConnectionStrings__DefaultConnection`, `ConnectionStrings__MongoDB`, `ConnectionStrings__Redis`,
`Jwt__Secret`, `RabbitMQ__Host/Username/Password`. Em produção vêm do ConfigMap `fcg-config` e do Secret
`fcg-secrets` (alimentado pelo Azure Key Vault). A API **recusa subir** se faltar algum segredo.

## Testes e pipeline

```bash
dotnet test tests/FCG.Users.Tests -c Release   # 24 testes: domínio, validação de senha, AuthService
```

`.github/workflows/ci-cd.yml`: build → testes → imagem versionada → Trivy → push no ACR → deploy no AKS (Rolling Update).
