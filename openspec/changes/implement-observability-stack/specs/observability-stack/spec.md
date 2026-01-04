# Observability Stack Specification

## ADDED Requirements

### Requirement: OpenTelemetry Collector como Hub Central
O sistema SHALL utilizar o OpenTelemetry Collector como ponto central de coleta de telemetria, recebendo dados via OTLP (gRPC na porta 4317 e HTTP na porta 4318) de todos os serviços .NET e roteando para backends apropriados (Jaeger para traces, Prometheus para métricas, Loki para logs).

#### Scenario: Coleta de telemetria via Collector
- **WHEN** um serviço .NET envia telemetria via OTLP para o Collector
- **THEN** o Collector recebe os dados nas portas 4317 (gRPC) ou 4318 (HTTP)
- **AND** processa os dados através de processors (resourcedetection, resource, batch, attributes)
- **AND** roteia traces para Jaeger, métricas para Prometheus, logs para Loki
- **AND** mantém compatibilidade exportando também para Aspire Dashboard

#### Scenario: Health check do Collector
- **WHEN** o Collector está em execução
- **THEN** expõe endpoint de health check na porta 13133
- **AND** expõe métricas próprias na porta 8889 para scraping do Prometheus

### Requirement: Rastreamento Distribuído com Jaeger
O sistema SHALL fornecer rastreamento distribuído de requisições entre microserviços através do Jaeger, permitindo visualização de spans hierárquicos, busca por serviço/operação/tags, análise de latências e mapa de dependências entre serviços.

#### Scenario: Visualização de traces no Jaeger
- **WHEN** uma requisição é processada através de múltiplos serviços
- **THEN** um trace completo é visível no Jaeger UI (http://localhost:16686)
- **AND** o trace contém spans hierárquicos mostrando o fluxo entre serviços
- **AND** é possível buscar traces por serviço, operação ou tags
- **AND** o trace inclui informações de latência por span

#### Scenario: Correlação de traces
- **WHEN** um trace é gerado com TraceId
- **THEN** o TraceId é propagado através de todos os serviços na cadeia de requisição
- **AND** é possível buscar o trace completo no Jaeger usando o TraceId
- **AND** logs relacionados podem ser filtrados no Loki usando o mesmo TraceId

### Requirement: Coleta de Métricas com Prometheus
O sistema SHALL coletar e armazenar métricas time-series através do Prometheus, permitindo queries PromQL, alertas configuráveis e visualização de métricas de performance e saúde dos serviços.

#### Scenario: Scraping de métricas
- **WHEN** o Prometheus está em execução
- **THEN** faz scraping das métricas do OpenTelemetry Collector a cada 15 segundos
- **AND** armazena métricas com retenção de 7 dias (configurável)
- **AND** expõe UI em http://localhost:9090 para queries PromQL

#### Scenario: Métricas de requisições HTTP
- **WHEN** requisições HTTP são processadas pelos serviços
- **THEN** métricas são coletadas incluindo taxa de requisições (QPS)
- **AND** latência (histograma com buckets para P50, P95, P99)
- **AND** distribuição de status codes HTTP
- **AND** métricas são acessíveis via queries PromQL como `rate(http_server_request_duration_count[5m])`

### Requirement: Centralização de Logs com Loki
O sistema SHALL centralizar logs estruturados através do Loki, permitindo queries LogQL, correlação com traces via TraceId, e filtragem por labels como service_name, level e deployment_environment.

#### Scenario: Ingestão de logs estruturados
- **WHEN** serviços .NET geram logs usando ILogger com structured logging
- **THEN** logs são enviados via OTLP para o Collector
- **AND** o Collector roteia logs para o Loki
- **AND** logs são indexados com labels automáticos (service_name, level, deployment_environment)
- **AND** logs são acessíveis via Grafana usando queries LogQL como `{service_name="api"}`

#### Scenario: Correlação de logs com traces
- **WHEN** um log é gerado durante o processamento de uma requisição
- **THEN** o log inclui o TraceId da requisição atual
- **AND** é possível filtrar logs no Loki usando o TraceId: `{service_name="api"} |= "TraceId=abc123"`
- **AND** é possível navegar do log para o trace no Jaeger usando o TraceId

### Requirement: Visualização Unificada com Grafana
O sistema SHALL fornecer visualização unificada de traces, métricas e logs através do Grafana, com datasources pré-configurados (Prometheus, Loki, Jaeger) e dashboards essenciais para monitoramento.

#### Scenario: Acesso ao Grafana
- **WHEN** o Grafana está em execução
- **THEN** a UI está acessível em http://localhost:3000
- **AND** credenciais padrão são admin/admin (configurável)
- **AND** datasources (Prometheus, Loki, Jaeger) são auto-provisionados via arquivos YAML

#### Scenario: Dashboard de overview
- **WHEN** um dashboard "Overview Geral" é acessado no Grafana
- **THEN** exibe taxa de requisições (QPS) por serviço
- **AND** latência P50, P95, P99
- **AND** taxa de erro
- **AND** métricas de CPU e memória por serviço

#### Scenario: Correlação entre sinais
- **WHEN** um trace é visualizado no Grafana (via datasource Jaeger)
- **THEN** é possível clicar em um span para ver logs relacionados
- **AND** é possível ver métricas de performance do serviço no mesmo período
- **AND** a correlação é feita via TraceId compartilhado

### Requirement: Configuração via Docker Compose
O sistema SHALL fornecer configuração completa da stack de observabilidade via `docker-compose.observability.yml`, permitindo iniciar/parar a stack independentemente dos serviços de aplicação.

#### Scenario: Inicialização da stack
- **WHEN** o comando `podman-compose -f docker-compose.observability.yml up -d` é executado
- **THEN** todos os serviços (otel-collector, jaeger, prometheus, loki, grafana) são iniciados
- **AND** serviços são conectados à network `bank-net` (external)
- **AND** volumes são criados para dados persistentes (prometheus, loki, grafana)
- **AND** health checks validam que serviços estão prontos

#### Scenario: Parada da stack
- **WHEN** o comando `podman-compose -f docker-compose.observability.yml down` é executado
- **THEN** todos os serviços são parados
- **AND** volumes são preservados (dados não são perdidos)
- **AND** serviços de aplicação continuam funcionando normalmente

### Requirement: Compatibilidade com Stack Atual
O sistema SHALL manter compatibilidade com a stack atual (Aspire Dashboard), permitindo que ambas as stacks funcionem em paralelo durante a transição.

#### Scenario: Exportação paralela
- **WHEN** o OpenTelemetry Collector está configurado
- **THEN** exporta telemetria tanto para Aspire Dashboard quanto para novos backends (Jaeger, Prometheus, Loki)
- **AND** serviços .NET não requerem alterações de código
- **AND** ambas as visualizações (Aspire e Grafana) recebem dados simultaneamente

### Requirement: Documentação de Observabilidade
O sistema SHALL fornecer documentação completa sobre a stack de observabilidade, incluindo arquitetura, URLs de acesso, credenciais, queries úteis e guia de troubleshooting.

#### Scenario: Documentação de arquitetura
- **WHEN** um desenvolvedor consulta `infra/OBSERVABILITY.md`
- **THEN** encontra diagrama da arquitetura completa
- **AND** descrição de cada componente e sua função
- **AND** instruções de como iniciar/parar a stack

#### Scenario: Guia de queries
- **WHEN** um desenvolvedor precisa fazer queries de telemetria
- **THEN** encontra exemplos de queries PromQL para métricas comuns
- **AND** exemplos de queries LogQL para logs
- **AND** instruções de como buscar traces no Jaeger

