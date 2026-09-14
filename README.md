# Aurora — API de Clínicas e Assinaturas

API REST em **ASP.NET Core (.NET 10)** do **Aurora**, plataforma em que **clínicas veterinárias gerenciam a agenda dos seus veterinários**. Este serviço responde pelo cadastro das clínicas e pela camada de **assinaturas**, que liberam recursos adicionais dentro do aplicativo, com persistência em **Oracle** via **Entity Framework Core** e cobrança recorrente pela **Stripe**.

> Challenge 2026 — 2º ano ADS
> Entrega da disciplina **Advanced Business Development with .NET**.

## Integrantes

| Nome | RM |
|---|---|
| Maicon | RM561279 |
| Gabriel | RM561551 |
| Charlles | RM566482 |
| Iago D. | RM565708 |

---

## 1. Descrição da solução

O Aurora organiza a agenda de atendimento das clínicas veterinárias: cada clínica cadastra seus veterinários e administra horários, encaixes e disponibilidade em um único lugar. O acesso base é gratuito, e um **plano de assinatura** libera os recursos avançados.

Esta API sustenta esse modelo de negócio:

- **Cadastro de clínicas** — a clínica é a titular da relação comercial na plataforma. É identificada por CNPJ, com e-mail de cobrança e a conta correspondente no Firebase Authentication.
- **Catálogo de planos** — os planos comercializados (`Basic`, `Premium`), cada um espelhando um *Price* configurado na Stripe. Criar um plano ou mudar um preço é operação de dado, não de deploy.
- **Assinaturas** — a associação entre a clínica e o plano contratado, guardando o ciclo de vida (`Incomplete → Active → PastDue → Canceled`), a vigência do período pago e o espelho da *Subscription* na Stripe. É o registro que o aplicativo consulta para decidir quais recursos liberar.
- **Checkout** — criação do *Customer* e da *Subscription* na Stripe, devolvendo o `clientSecret` que o aplicativo usa para confirmar o pagamento.

### Benefícios para o negócio

| Problema | O que a solução entrega |
|---|---|
| Receita recorrente controlada em planilha, com inadimplência descoberta tarde | Cobrança automatizada na Stripe, com o status de cada assinatura refletido no banco |
| App sem critério confiável para liberar recursos pagos | Fonte única de verdade sobre clínica, plano, status e vigência, consultável por API |
| Mudança de preço ou criação de plano exigia alteração no código | Planos viram dado: preço e *price id* editáveis via CRUD, sem novo deploy |
| Cadastro duplicado de clínica gerando cobrança em duplicidade | CNPJ e UID do Firebase únicos, validados na aplicação e no banco |
| Exclusão acidental quebraria clínicas ativas | Regras de negócio e chaves estrangeiras impedem remover clínica ou plano em uso |

---

## 2. O core do domínio

O modelo é composto por **três tabelas de negócio**, todas centrais ao produto — não há tabelas auxiliares de cadastro genérico (cidade, estado, perfil de acesso etc.):

```
   TB_CLINIC                    TB_SUBSCRIPTION_PLAN
   (quem contrata)              (o que é vendido)
        │                                │
        │ 1                            1 │
        │                                │
        └────< N  TB_CLINIC_SUBSCRIPTION  N >────┘
                  (o contrato vigente)
```

| Tabela | Papel no domínio | Por que é core |
|---|---|---|
| `TB_CLINIC` | A clínica veterinária | É o *tenant* da plataforma: toda agenda, veterinário e recurso pago pertence a uma clínica. Sem ela não existe produto |
| `TB_SUBSCRIPTION_PLAN` | O plano comercializado | Define o que é vendido e por quanto; é o catálogo que sustenta a receita recorrente |
| `TB_CLINIC_SUBSCRIPTION` | O contrato vigente | Associativa entre as duas pontas. É a tabela consultada para decidir se um recurso está liberado |

**A clínica é a assinante — não um tutor nem um veterinário individual.** A assinatura aponta para `CLINIC_ID` por chave estrangeira, e as regras de integridade acompanham esse desenho:

- Não se exclui uma clínica que possua assinaturas (`409 Conflict`);
- Não se exclui um plano que possua assinaturas (`409 Conflict`);
- Uma clínica desativada não contrata novos planos (`409 Conflict`);
- CNPJ e UID do Firebase são únicos por clínica (índices únicos no Oracle);
- Uma assinatura cancelada não muda mais de status (`409 Conflict`).

O **CRUD completo** está implementado nas três tabelas, e o relacionamento é exercitado pelos testes de integração e pelas consultas de evidência em [`script_bd.sql`](script_bd.sql).

---

## 3. Arquitetura

```
Aurora/
├── Aurora.slnx                       Solution (API + 2 projetos de teste)
├── script_bd.sql                     DDL completo das tabelas, com comentários e carga inicial
├── README.md
├── Aurora/                           Projeto da API
│   ├── Domain/Entities/              Clinic, SubscriptionPlan, ClinicSubscription
│   ├── Payments/Application/
│   │   ├── Dtos/                     Contratos de entrada e saída da API (records)
│   │   ├── Interfaces/               Repositórios (portas) e ISubscriptionService
│   │   └── Services/                 Casos de uso (CRUD + regras de negócio)
│   ├── Payments/                     StripeSubscriptionService (integração externa)
│   ├── Infrastructure/Data/          AuroraDbContext, configurations, migrations, repositórios
│   ├── Controllers/                  Endpoints REST
│   └── Program.cs                    Composição da aplicação (DI)
└── tests/
    ├── Aurora.Tests.Unit/            Testes unitários (Domínio e Aplicação)
    └── Aurora.Tests.Integration/     Testes de integração (WebApplicationFactory)
```

O fluxo segue camadas: **Controller → Service (caso de uso) → Repository (porta) → EF Core → Oracle**.

- **Domínio rico** — as entidades têm construtores validados e propriedades com `private set`; não é possível criar uma clínica sem CNPJ válido ou um plano com preço negativo. Mudanças de estado passam por métodos de negócio (`Activate`, `UpdateStatus`, `ChangePlan`, `Cancel`).
- **Inversão de dependência** — os serviços dependem de interfaces de repositório, o que permite testá-los isoladamente com mocks. As implementações concretas com EF Core ficam na infraestrutura.
- **Injeção de dependência** — todo o grafo é registrado em [`Program.cs`](Aurora/Program.cs) com tempo de vida `Scoped`.
- **DTOs** — as entidades nunca cruzam a fronteira HTTP: entrada e saída usam `records` dedicados em `Payments/Application/Dtos`.

---

## 4. Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (`dotnet --version` ≥ 10.0.100)
- Ferramenta EF Core: `dotnet tool install --global dotnet-ef`
- Acesso ao Oracle da FIAP (`oracle.fiap.com.br:1521/orcl`) — pela rede da faculdade ou VPN

---

## 5. Como executar

### 5.1 Clonar o repositório

```bash
git clone https://github.com/MCGI-PW/StripeAurora.git
```

```bash
cd StripeAurora
```

### 5.2 Configurar as credenciais

**Nenhuma credencial fica no repositório.** A connection string e as chaves da Stripe são lidas de *user secrets* em desenvolvimento:

```bash
dotnet user-secrets --project Aurora set "ConnectionStrings:OracleConnection" "User Id=SEU_RM;Password=SUA_SENHA;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))(CONNECT_DATA=(SID=orcl)))"
```

```bash
dotnet user-secrets --project Aurora set "Stripe:SecretKey" "sk_test_SUA_CHAVE"
```

A aplicação falha na inicialização, com mensagem explícita, se a connection string não estiver configurada.

### 5.3 Criar as tabelas (migrations)

```bash
dotnet ef database update --project Aurora
```

A migration `20260914014332_InitialCreate` cria as três tabelas, chaves, índices e comentários, e insere a carga inicial (2 clínicas, 2 planos e 2 assinaturas).

O DDL equivalente, para leitura ou execução manual, está em [`script_bd.sql`](script_bd.sql).

> **Nota Oracle.** A migration reaplica os comentários com `COMMENT ON` em SQL puro no fim do `Up()`. O provider Oracle emite os comentários das migrations como literais `N'...'`, e o banco da FIAP os grava byte a byte — o texto ficaria ilegível em `USER_TAB_COMMENTS` / `USER_COL_COMMENTS`.

### 5.4 Rodar a API

```bash
dotnet run --project Aurora
```

A API sobe em `http://localhost:5287` (e `https://localhost:7286` no perfil HTTPS).

### 5.5 Documentação OpenAPI

Em ambiente de desenvolvimento, o contrato gerado por `Microsoft.AspNetCore.OpenApi` fica em:

```
http://localhost:5287/openapi/v1.json
```

O arquivo pode ser importado no Swagger UI, Postman ou Insomnia. Todos os endpoints estão anotados com comentários XML `<summary>` e com `[ProducesResponseType]`.

---

## 6. Endpoints

Base local: `http://localhost:5287`

### 6.1 Clínicas — `/api/clinics` (CRUD completo)

| Método | Rota | Operação | Respostas |
|---|---|---|---|
| `GET` | `/api/clinics` | Consulta todas | `200` |
| `GET` | `/api/clinics/{id}` | Consulta por id | `200`, `404` |
| `POST` | `/api/clinics` | Inclusão | `201`, `400`, `409` |
| `PUT` | `/api/clinics/{id}` | Alteração | `200`, `400`, `404` |
| `DELETE` | `/api/clinics/{id}` | Exclusão | `204`, `404`, `409` |

`409` no `POST` = CNPJ já cadastrado. `409` no `DELETE` = a clínica possui assinaturas.

### 6.2 Planos — `/api/plans` (CRUD completo)

| Método | Rota | Operação | Respostas |
|---|---|---|---|
| `GET` | `/api/plans` | Consulta todos | `200` |
| `GET` | `/api/plans/{id}` | Consulta por id | `200`, `404` |
| `POST` | `/api/plans` | Inclusão | `201`, `400` |
| `PUT` | `/api/plans/{id}` | Alteração | `200`, `400`, `404` |
| `DELETE` | `/api/plans/{id}` | Exclusão | `204`, `404`, `409` |

### 6.3 Assinaturas — `/api/subscriptions` (CRUD completo)

| Método | Rota | Operação | Respostas |
|---|---|---|---|
| `GET` | `/api/subscriptions` | Consulta todas | `200` |
| `GET` | `/api/subscriptions/{id}` | Consulta por id | `200`, `404` |
| `POST` | `/api/subscriptions` | Inclusão (liga clínica ↔ plano) | `201`, `400`, `404`, `409` |
| `PUT` | `/api/subscriptions/{id}` | Alteração de status/vigência/plano | `200`, `404`, `409` |
| `DELETE` | `/api/subscriptions/{id}` | Exclusão | `204`, `404` |

### 6.4 Checkout Stripe — `/api/checkout`

| Método | Rota | Operação |
|---|---|---|
| `POST` | `/api/checkout` | Cria Customer + Subscription na Stripe e devolve o `clientSecret` |
| `DELETE` | `/api/checkout/{stripeSubscriptionId}` | Cancela a assinatura na Stripe |

### 6.5 Exemplo de ciclo completo

Inclusão de clínica:

```bash
curl -X POST http://localhost:5287/api/clinics -H "Content-Type: application/json" -d '{"name":"Clinica Bem Estar Pet","cnpj":"45.678.901/0001-23","email":"contato@bemestarpet.com.br","firebaseUid":"uid-clinica-bem-estar"}'
```

Alteração da clínica:

```bash
curl -X PUT http://localhost:5287/api/clinics/{ID_DA_CLINICA} -H "Content-Type: application/json" -d '{"name":"Clinica Bem Estar Pet 24h","cnpj":"45678901000123","email":"financeiro@bemestarpet.com.br","active":true}'
```

Contratação de um plano:

```bash
curl -X POST http://localhost:5287/api/subscriptions -H "Content-Type: application/json" -d '{"clinicId":"{ID_DA_CLINICA}","planId":"{ID_DO_PLANO}","stripeCustomerId":"cus_demo","stripeSubscriptionId":"sub_demo"}'
```

Ativação da assinatura (`status`: 0 = Incomplete, 1 = Active, 2 = PastDue, 3 = Canceled):

```bash
curl -X PUT http://localhost:5287/api/subscriptions/{ID_DA_ASSINATURA} -H "Content-Type: application/json" -d '{"status":1,"currentPeriodEnd":"2026-12-31T23:59:59Z","planId":"{ID_DO_PLANO}"}'
```

Exclusão:

```bash
curl -X DELETE http://localhost:5287/api/subscriptions/{ID_DA_ASSINATURA}
```

As consultas SQL para evidenciar cada operação diretamente no banco estão comentadas no fim de [`script_bd.sql`](script_bd.sql).

---

## 7. Boas práticas REST aplicadas

| Prática | Como aparece no código |
|---|---|
| Rotas por recurso, no plural | `/api/clinics`, `/api/plans`, `/api/subscriptions` |
| Verbos HTTP corretos | `GET` consulta, `POST` inclui, `PUT` altera, `DELETE` exclui |
| Códigos de status adequados | `201 Created` com `Location` via `CreatedAtAction`, `204 No Content` na exclusão, `400`/`404`/`409` nos erros |
| Erros padronizados | Todas as falhas devolvem `ProblemDetails` (RFC 7807) com título e detalhe |
| Restrição de rota | `{id:guid}` valida o formato do identificador antes de chegar ao controller |
| Separação de responsabilidades | O controller só traduz exceções de domínio em status HTTP; a regra vive no serviço |
| Assincronismo de ponta a ponta | Todos os métodos são `async` e propagam `CancellationToken` até o EF Core |
| Contrato documentado | `[ProducesResponseType]`, `[Produces("application/json")]` e comentários XML alimentam o OpenAPI |

O mapeamento de exceções de domínio para HTTP é uniforme nos três controllers:

| Exceção | Status |
|---|---|
| `ArgumentException` / `ArgumentOutOfRangeException` (validação de entidade) | `400 Bad Request` |
| `ResourceNotFoundException` | `404 Not Found` |
| `BusinessRuleException` | `409 Conflict` |

---

## 8. Testes automatizados

Os testes seguem o padrão **AAA (Arrange, Act, Assert)** e a nomenclatura `Metodo_Cenario_ResultadoEsperado`, organizados em dois projetos separados por camada.

### 8.1 Executar

Toda a suíte:

```bash
dotnet test
```

Somente unitários:

```bash
dotnet test tests/Aurora.Tests.Unit
```

Somente integração:

```bash
dotnet test tests/Aurora.Tests.Integration
```

Com relatório de cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### 8.2 O que está coberto

| Projeto | Framework | Casos | Cobre |
|---|---|---|---|
| `Aurora.Tests.Unit` | xUnit + NSubstitute | 42 | **Domínio**: construtores e validações das três entidades (CNPJ de 14 dígitos, e-mail, nome, preço), `Update`, `Activate`/`Deactivate`, `UpdateStatus`, `ChangePlan`, `Cancel`. **Aplicação**: os três serviços de caso de uso com repositórios mockados — incluindo os caminhos de erro (`ResourceNotFoundException`, `BusinessRuleException`) e a verificação de que nada é persistido quando a regra falha |
| `Aurora.Tests.Integration` | xUnit + `WebApplicationFactory` | 27 | Fluxo HTTP completo dos três CRUDs (`201`/`200`/`204`), tratamento de erros (`400`, `404`, `409`), proteção das duas chaves estrangeiras e bloqueio de contratação por clínica desativada |

**Organização e contexto compartilhado.** Cada projeto usa *fixtures* com `ICollectionFixture`: `PlanFixture` fabrica entidades válidas para os testes unitários; `AuroraWebApplicationFactory` sobe a API uma única vez e é reaproveitada por todas as classes de integração.

Os testes de integração substituem **apenas o provider do EF Core** por um banco em memória — o pipeline, os controllers e o DI são os reais. Isso mantém a suíte executável em qualquer máquina, sem depender do Oracle da FIAP.

Resultado atual:

```
Aprovado! – Com falha: 0, Aprovado: 42, Total: 42 — Aurora.Tests.Unit
Aprovado! – Com falha: 0, Aprovado: 27, Total: 27 — Aurora.Tests.Integration
```

---

## 9. Banco de dados

**Servidor:** Oracle da FIAP — `oracle.fiap.com.br:1521`, SID `orcl`. O mapeamento é feito por *Fluent API*, em classes `IEntityTypeConfiguration` separadas por entidade.

### `TB_CLINIC`

| Coluna | Tipo | Descrição |
|---|---|---|
| `ID` | `RAW(16)` **PK** | Chave primária (GUID gerado pela aplicação) |
| `NAME` | `NVARCHAR2(150)` | Razão social ou nome fantasia |
| `CNPJ` | `NVARCHAR2(14)` **UK** | CNPJ, somente dígitos |
| `EMAIL` | `NVARCHAR2(150)` | E-mail administrativo usado na cobrança |
| `FIREBASE_UID` | `NVARCHAR2(128)` **UK** | Conta da clínica no Firebase Authentication |
| `ACTIVE` | `NUMBER(1)` | `1` ativa, `0` desativada |

### `TB_SUBSCRIPTION_PLAN`

| Coluna | Tipo | Descrição |
|---|---|---|
| `ID` | `RAW(16)` **PK** | Chave primária (GUID gerado pela aplicação) |
| `TIER` | `NVARCHAR2(20)` | `Basic` ou `Premium` |
| `NAME` | `NVARCHAR2(120)` | Nome comercial do plano |
| `PRICE` | `NUMBER(18,2)` | Preço mensal em reais |
| `STRIPE_PRICE_ID` | `NVARCHAR2(120)` **UK** | *Price* correspondente na Stripe |
| `ACTIVE` | `NUMBER(1)` | `1` disponível, `0` descontinuado |

### `TB_CLINIC_SUBSCRIPTION`

| Coluna | Tipo | Descrição |
|---|---|---|
| `ID` | `RAW(16)` **PK** | Chave primária (GUID gerado pela aplicação) |
| `CLINIC_ID` | `RAW(16)` **FK** | → `TB_CLINIC.ID` (`RESTRICT`) |
| `PLAN_ID` | `RAW(16)` **FK** | → `TB_SUBSCRIPTION_PLAN.ID` (`RESTRICT`) |
| `STRIPE_CUSTOMER_ID` | `NVARCHAR2(120)` | *Customer* correspondente na Stripe |
| `STRIPE_SUBSCRIPTION_ID` | `NVARCHAR2(120)` **UK** | *Subscription* correspondente na Stripe |
| `STATUS` | `NVARCHAR2(20)` | `Incomplete`, `Active`, `PastDue`, `Canceled` |
| `CURRENT_PERIOD_END` | `TIMESTAMP(7)` | Fim do período pago corrente |

Todas as tabelas e colunas têm comentários no dicionário de dados (`USER_TAB_COMMENTS` / `USER_COL_COMMENTS`).

### Carga inicial

| Tabela | Registros |
|---|---|
| `TB_CLINIC` | `Clinica Vida Animal` (CNPJ 12345678000190) e `Clinica Patas Felizes` (CNPJ 98765432000155) |
| `TB_SUBSCRIPTION_PLAN` | `Aurora Basico` (R$ 29,90) e `Aurora Premium` (R$ 59,90) |
| `TB_CLINIC_SUBSCRIPTION` | Vida Animal → Premium, `Active`; Patas Felizes → Básico, `Incomplete` |

Os `Guid` do seed são fixos, para que reaplicar a migration seja idempotente.

---

## 10. Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 / ASP.NET Core |
| ORM | Entity Framework Core 10 + `Oracle.EntityFrameworkCore` |
| Banco | Oracle (FIAP) |
| Pagamentos | `Stripe.net` |
| Documentação | `Microsoft.AspNetCore.OpenApi` |
| Testes | xUnit, NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory |

---

## 11. Segurança de credenciais

Nenhum usuário, senha ou token está no código-fonte ou no `appsettings.json`. Os valores vêm de:

| Ambiente | Origem |
|---|---|
| Desenvolvimento local | `dotnet user-secrets` |
| Execução fora da máquina do desenvolvedor | Variáveis de ambiente (`ConnectionStrings__OracleConnection`, `Stripe__SecretKey`) |

O `.gitignore` bloqueia `bin/`, `obj/`, `.vs/` e `logs/`.

---

> Este README cobre a entrega de **Advanced Business Development with .NET**. O repositório também contém a camada de containerização e observabilidade (Dockerfile, health checks, Serilog e OpenTelemetry), que pertence à entrega de DevOps e é documentada à parte.
