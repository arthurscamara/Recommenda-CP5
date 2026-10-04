# Recommenda — CP5

API REST para descoberta musical. Evolucao do CP3/CP4 com **health checks, logs com traceId e testes xUnit (CP4)** e **versionamento de API, paginacao e rate limit (CP5)**.

---

## Integrantes

| Nome | RM |
|------|----|
| Arthur Câmara | 562310 |

---

## Dominio

Plataforma de descoberta musical que permite cadastrar artistas, albuns, faixas e generos, alem de registrar avaliacoes de usuarios sobre albuns.

## SGBD

MySQL 8 via `Pomelo.EntityFrameworkCore.MySql` (herdado do CP2).
Connection string em `Recommenda.API/appsettings.json`, chave `ConnectionStrings:RecommendaMySQL`.
A versao do servidor e fixa em `Database:MySqlVersion` (padrao `8.0.36`) para que a API suba mesmo com o banco fora do ar e o `/health` consiga responder 503.

---

## Estrutura da solucao

```
Recommenda.sln
├── Recommenda.Domain              entidades, regras de negocio, excecoes de dominio
├── Recommenda.Application         DTOs, servicos de aplicacao, interfaces de repositorio, paginacao
├── Recommenda.Infrastructure      EF Core (DbContext, migrations, repositorios, GetPaged)
├── Recommenda.API                 controllers, health checks, logs, versionamento, rate limit
└── tests
    ├── Recommenda.Domain.Tests        xUnit, referencia SOMENTE o Domain (sem mock)
    └── Recommenda.Application.Tests   xUnit + Moq, referencia Application (sem API, sem banco)
```

---

## Como executar

Pre-requisitos: .NET 10 SDK e MySQL rodando. Ajuste a senha em `Recommenda.API/appsettings.json` (ou use user-secrets; nao commitar credenciais reais).

```bash
dotnet restore
dotnet run --project Recommenda.API
```

As migrations sao aplicadas no startup. Se o banco estiver fora do ar, a API sobe mesmo assim, loga o erro e o `/health` responde 503.

| O que | URL |
|-------|-----|
| Swagger (Development) | http://localhost:5283/swagger |
| Health check | http://localhost:5283/health |
| Listagem v1 (deprecada) | http://localhost:5283/api/album?api-version=1.0 |
| Listagem v2 (paginada) | http://localhost:5283/api/album?page=1&pageSize=20 |

---

## CP5 — Versionamento (recurso: Album)

Pacotes: `Asp.Versioning.Mvc` e `Asp.Versioning.Mvc.ApiExplorer`.

- `DefaultApiVersion = 2.0`, `AssumeDefaultVersionWhenUnspecified = true`, `ReportApiVersions = true`.
- Leitores combinados: query string `api-version` e header `X-Api-Version`.
- **v1.0 (deprecada)**: `GET /api/album` devolve o array completo (contrato do CP3).
- **v2.0 (atual)**: `GET /api/album` devolve o envelope paginado.
- As duas versoes chamam o **mesmo** `IAlbumService` (`GetAll` na v1, `GetPaged` na v2). Nenhuma regra duplicada.

Como o cliente escolhe a versao:

```bash
# v1 por query string -> array
curl "http://localhost:5283/api/album?api-version=1.0"

# v1 por header -> o mesmo array
curl -H "X-Api-Version: 1.0" "http://localhost:5283/api/album"

# sem versao -> cai na 2.0 (envelope)
curl "http://localhost:5283/api/album"
```

As respostas do recurso versionado trazem os headers:

```
api-supported-versions: 2.0
api-deprecated-versions: 1.0
```

Os demais endpoints do Album (`GET /api/album/{id}`, `GET /api/album/artist/{artistId}`, `POST`, `DELETE`) existem **nas duas versoes**. Como o padrao e 2.0, a chamada de escrita pode ser feita sem informar versao:

```bash
curl -X POST http://localhost:5283/api/album -H "Content-Type: application/json" \
  -d '{"title":"No na Orelha","releaseDate":"2011-07-12T00:00:00","artistId":"<id-do-artista>"}'
```

**Outros recursos** (Artist, Track, Genre, AlbumRating) estao marcados com `[ApiVersionNeutral]`: continuam funcionando com ou sem versao e aparecem nos dois documentos do Swagger. So o Album foi versionado, como pede o enunciado.

**Swagger**: um documento por versao (`GroupNameFormat = 'v'VVVV` -> `v1.0` e `v2.0`). A UI tem o seletor de versoes; o grupo v1.0 aparece como "V1.0 (DEPRECADA)", com a descricao avisando a deprecacao e a operacao marcada como deprecated.

---

## CP5 — Paginacao (so na listagem v2)

| Parametro | Padrao | Regra |
|-----------|--------|-------|
| `page` | 1 | inteiro >= 1 |
| `pageSize` | 20 | inteiro de 1 a 100 |

- `page < 1` ou `pageSize` fora de 1–100 -> **400** (Problem Details com a regra na mensagem).
- Pagina alem do total -> **200** com `items: []`.
- Ordenacao estavel: `Title`, desempate por `Id`.

Resposta 200:

```json
{
  "page": 1,
  "pageSize": 2,
  "totalItems": 5,
  "totalPages": 3,
  "hasPrevious": false,
  "hasNext": true,
  "items": [ { "id": "...", "title": "...", "releaseDate": "...", "artistId": "...", "coverUrl": "" } ]
}
```

Erro 400:

```json
{
  "type": "about:blank",
  "title": "Parametros de paginacao invalidos",
  "status": 400,
  "detail": "O parametro 'pageSize' deve estar entre 1 e 100 (recebido: 9999).",
  "instance": "/api/album",
  "traceId": "0HN..."
}
```

Onde cada camada entra:

- **Controller** (`AlbumController.GetAllV2`): le `page` e `pageSize` da query.
- **Application**: `PageRequest.Create` valida a faixa (lanca `InvalidPageRequestException`), `AlbumService.GetPaged` pede a pagina ao repositorio e monta o envelope `PagedResponse<T>` (DTO na Application).
- **Infrastructure**: `AlbumRepository.GetPaged` (e `Repository<T>.GetPaged` no generico) faz `Count` + `OrderBy` + `Skip` + `Take` no `IQueryable` e so entao `ToList()` — o corte e no SQL (`LIMIT/OFFSET`), nunca em memoria.
- A v1 continua chamando `GetAll()` e devolvendo a lista inteira (nao paginar a v1 e justamente o que preserva o contrato antigo).

---

## CP5 — Rate limit

Middleware nativo `Microsoft.AspNetCore.RateLimiting`, politica **fixed window particionada por IP**.

| Politica | Onde | Limite | Janela |
|----------|------|--------|--------|
| `escrita` | `POST /api/album` e `POST /api/albumrating` | 10 requisicoes | 1 minuto |
| `leitura` | `GET /api/album` v2 (listagem paginada) | 60 requisicoes | 1 minuto |

Ao estourar: **429** com header `Retry-After` (segundos) e corpo Problem Details:

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
  "title": "Limite de requisicoes excedido",
  "status": 429,
  "detail": "Limite de requisicoes atingido para este endpoint. Tente novamente em 42 segundos.",
  "instance": "/api/album",
  "retryAfterSeconds": 42,
  "traceId": "0HN..."
}
```

`UseRateLimiter()` fica depois de `UseExceptionHandler()` e antes de `MapControllers()`. O `GET /health` tem `DisableRateLimiting()` e nao divide o teto: depois do 429 no POST, `/health` continua 200.

Para testar no Swagger: dispare o `POST /api/album` 11 vezes em menos de 1 minuto.

---

## CP4 — Health checks

`GET /health` e o unico endpoint de health (nao aparece no Swagger). Checks registrados em `AddRecommendaHealthChecks()` (`Extensions/HealthCheckExtensions.cs`):

| Check | O que verifica | Falha vira |
|-------|----------------|------------|
| `self` | processo no ar | — (sempre Healthy) |
| `mysql` | **abordagem (A)**: `AddDbContextCheck<RecommendaContext>` (pacote `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`), executa `CanConnectAsync` no MySQL | Unhealthy |
| `fiap-site` | GET em `https://www.fiap.com.br` (configuravel em `HealthChecks:ExternalUrl`), timeout 5s | Degraded |

Status HTTP: Healthy -> 200, Degraded -> 200, Unhealthy -> 503.

Sobre a URL externa: um check Unhealthy derruba o relatorio inteiro (503) e um orquestrador tiraria a API do ar. Como a API nao depende do site da FIAP para atender, o check foi registrado com `failureStatus: Degraded` — se o site cair, `/health` fica `Degraded` e continua 200.

Resposta (writer JSON proprio, `HealthChecks/HealthCheckResponseWriter.cs`):

```json
{
  "status": "Healthy",
  "totalDurationMs": 48.31,
  "timestamp": "2026-10-04T21:00:00+00:00",
  "traceId": "0HN...",
  "checks": [
    { "name": "self",      "status": "Healthy", "durationMs": 0.02,  "description": "API em execucao.", "tags": ["live"], "error": null },
    { "name": "mysql",     "status": "Healthy", "durationMs": 12.5,  "description": null, "tags": ["db","ready"], "error": null },
    { "name": "fiap-site", "status": "Healthy", "durationMs": 40.1,  "description": "https://www.fiap.com.br respondeu 200.", "tags": ["external"], "error": null }
  ]
}
```

`error` (mensagem da excecao) so e preenchido em Development.

Simulando falha do banco: pare o MySQL (`net stop MySQL80` num terminal como administrador, ou pare o container) e chame `curl -i http://localhost:5283/health` -> **503** com `mysql: Unhealthy`. Evidencia em `docs/evidencias/`.

---

## CP4 — Observabilidade (logs)

- `ILogger<T>` nativo, console com escopos (`SingleLine`, `IncludeScopes`).
- `TraceIdLoggingMiddleware` abre um escopo `TraceId = HttpContext.TraceIdentifier` em toda requisicao e devolve o mesmo valor no header `X-Trace-Id`.
- Fluxos de escrita com log de inicio, sucesso e falha de negocio, com propriedades nomeadas: `POST /api/album` e `POST /api/albumrating`.
- `GlobalExceptionHandler` loga a excecao em nivel **Error** com `ExceptionType`, `Method`, `Path`, `StatusCode` e `TraceId`; o mesmo `traceId` vai em `ProblemDetails.Extensions`. Em Production a resposta nao tem stack trace (so o log tem).

Exemplo de log de um POST:

```
2026-10-04 18:00:01 info: Recommenda.API.Controllers.AlbumController[0] => TraceId:0HN7...:00000001 => ... Iniciando criacao de album No na Orelha para o artista 3fa8... TraceId=0HN7...:00000001
2026-10-04 18:00:01 info: Recommenda.API.Controllers.AlbumController[0] => TraceId:0HN7...:00000001 => ... Album 9c1e... criado com sucesso para o artista 3fa8... TraceId=0HN7...:00000001
```

---

## Testes (xUnit)

```bash
dotnet test
```

(rodar na raiz, onde esta o `Recommenda.sln`)

**Recommenda.Domain.Tests** — referencia so o Domain, sem mock, AAA:

- `AlbumRatingTests`: nota 1..5 (`[Fact]` caminho feliz, `[Theory]` com 0, 6, -1, 100 -> `DomainException`).
- `AlbumTests`: titulo obrigatorio e data de lancamento >= 1877.
- `TrackTests`: duracao formatada `mm:ss` e duracao/numero da faixa positivos.

**Recommenda.Application.Tests** — Moq nas interfaces de repositorio:

- `AlbumRatingServiceTests`: usuario inexistente, album inexistente e avaliacao duplicada -> excecao mapeada e `Create` com `Times.Never`; caminho feliz com `Times.Once`.
- `AlbumServiceTests`: artista inexistente -> `ResourceNotFoundException` e `Times.Never`; caminho feliz `Times.Once`; paginacao com `[Theory]` para `page`/`pageSize` invalidos (repositorio nunca consultado), `[Theory]` na faixa valida, totais do envelope e pagina alem do total.

---

## Mapeamento de excecoes para HTTP

| Excecao | Status HTTP | Titulo |
|---------|-------------|--------|
| InvalidPageRequestException | 400 | Parametros de paginacao invalidos |
| ArgumentNullException | 400 | Requisicao invalida |
| ArgumentException | 400 | Requisicao invalida |
| DomainException (generica) | 400 | Nao foi possivel concluir a operacao |
| InvalidOperationException | 400 | Nao foi possivel concluir a operacao |
| ResourceNotFoundException | 404 | Recurso nao encontrado |
| KeyNotFoundException | 404 | Recurso nao encontrado |
| ConflictException | 409 | Conflito |
| UnauthorizedAccessException | 401 | Nao autorizado |
| (rate limit) | 429 | Limite de requisicoes excedido |
| Demais excecoes | 500 | Erro interno do servidor |

Todas as respostas de erro seguem RFC 7807 (`application/problem+json`). Em producao, detalhes internos e stack trace nao sao expostos. As regras das entidades agora lancam `DomainException` (antes era `Exception` generica, que caia em 500).

---

## Repositorio generico

`IRepository<T>` (Application) define: `GetAll`, `GetPaged`, `GetById`, `Add`, `Delete`, `ExistsById`. `Repository<T>` (Infrastructure) implementa com EF Core (`AsNoTracking` nas leituras). O `GenreController` usa `IRepository<Genre>` direto.

```csharp
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

---

## Evidencias (`/docs`)

Com a API e o MySQL no ar, rode na raiz:

```powershell
powershell -ExecutionPolicy Bypass -File docs\coletar-evidencias.ps1
```

O script gera em `docs/evidencias/`: `/health` Healthy, seed de albuns, 429 com `Retry-After`, `/health` 200 depois do 429, GET v1 (query e header), GET v2 (sem versao e com versao), pagina 1 e 2 com `pageSize=2`, 400 de `page=0` e `pageSize=9999`, pagina enorme com `items: []`, documentos Swagger v1.0/v2.0 e um 404 com `traceId`.

Complementos manuais:

- `dotnet test > docs/evidencias/dotnet-test.txt`
- `/health` com o MySQL parado -> `docs/evidencias/health-unhealthy.txt`
- Print do Swagger com os dois grupos -> `docs/evidencias/swagger.png`
- Trecho do console da API com o POST e o traceId -> `docs/evidencias/log-post.txt`
