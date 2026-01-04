## Observabilidade e health

### Variáveis e configuração
- `OpenTelemetry:Otlp:Endpoint`: endpoint OTLP (ex.: `http://otel-collector:4317`).
- `OpenTelemetry:ServiceEnvironment`: ambiente para resource attributes (fallback para `ASPNETCORE_ENVIRONMENT`).
- `OpenTelemetry:ServiceVersion`: versão do serviço (fallback para versão do assembly).
- `Serilog:OpenSearch:Uri`: endpoint do cluster OpenSearch/Elasticsearch.
- `Serilog:OpenSearch:IndexFormat`: formato do índice (padrão `mynewlittlebank-logs-{0:yyyy.MM.dd}`).
- `ConnectionStrings:DefaultConnection`: conexão PostgreSQL.
- `RabbitMQ:*`: host, porta, vhost, usuário, senha, TLS (`SslEnabled`, `SslServerName`).

### Endpoints expostos
- `/health/live`: checa apenas `self` (usado para liveness).
- `/health/ready`: checa PostgreSQL e RabbitMQ.
- `/metrics`: Prometheus scraping endpoint.

### Fluxo E2E (local)
1. Suba o stack de observabilidade (OTLP collector + Jaeger/Tempo + Prometheus + OpenSearch).
2. Configure variáveis (`OpenTelemetry:Otlp:Endpoint`, `Serilog:OpenSearch:Uri`, `ConnectionStrings:DefaultConnection`, `RabbitMQ:*`).
3. Rode um serviço (ex.: `dotnet run --project src/Services.Pix`).
4. Gere tráfego (Mock.Transactions publica mensagens). Verifique:
   - Traces correlacionados publisher → consumidores no Jaeger/Tempo.
   - Métricas em Prometheus (`/metrics` nos serviços).
   - Logs estruturados ECS no OpenSearch com `service` e correlação.
