## ADDED Requirements

### Requirement: Stack de Observabilidade Completa
O sistema SHALL fornecer uma stack completa de observabilidade composta por Grafana, Prometheus, Loki, Tempo e Pyroscope para armazenamento, consulta e visualização de telemetria coletada pelo OpenTelemetry Collector.

#### Scenario: Stack de observabilidade disponível e funcional
- **WHEN** a stack de observabilidade é iniciada via `docker-compose.observability.yml`
- **THEN** todos os serviços (Grafana, Prometheus, Loki, Tempo, Pyroscope) iniciam corretamente
- **AND** todos os serviços estão acessíveis nas portas configuradas
- **AND** health checks de todos os serviços retornam status saudável

#### Scenario: OpenTelemetry Collector roteia telemetria para backends
- **WHEN** serviços .NET enviam telemetria via OTLP para o OpenTelemetry Collector
- **THEN** traces são roteados para Tempo via OTLP gRPC
- **AND** logs são roteados para Loki via OTLP HTTP
- **AND** métricas são expostas para Prometheus via endpoint `/metrics`
- **AND** profiling pode ser enviado para Pyroscope via HTTP

### Requirement: Grafana como Plataforma Central
O sistema SHALL fornecer Grafana como plataforma central de visualização com datasources pré-configurados para Prometheus, Loki, Tempo e Pyroscope.

#### Scenario: Grafana inicia com datasources configurados
- **WHEN** Grafana é iniciado
- **THEN** datasource Prometheus está configurado apontando para `http://prometheus:9090`
- **AND** datasource Loki está configurado apontando para `http://loki:3100`
- **AND** datasource Tempo está configurado apontando para `http://tempo:3200`
- **AND** datasource Pyroscope está configurado apontando para `http://pyroscope:4040`
- **AND** todos os datasources são acessíveis via interface do Grafana

#### Scenario: Acesso ao Grafana
- **WHEN** usuário acessa `http://localhost:3000`
- **THEN** interface do Grafana é exibida
- **AND** credenciais padrão (admin/admin) permitem login
- **AND** Explore permite consultar Prometheus, Loki e Tempo

### Requirement: Prometheus para Métricas
O sistema SHALL fornecer Prometheus para armazenamento e consulta de métricas time-series coletadas do OpenTelemetry Collector.

#### Scenario: Prometheus coleta métricas do Collector
- **WHEN** OpenTelemetry Collector expõe métricas em `http://otel-collector:8889/metrics`
- **THEN** Prometheus faz scrape dessas métricas a cada 15 segundos
- **AND** métricas são armazenadas com retenção de 7 dias
- **AND** métricas são consultáveis via PromQL na interface do Prometheus

#### Scenario: Acesso ao Prometheus
- **WHEN** usuário acessa `http://localhost:9090`
- **THEN** interface do Prometheus é exibida
- **AND** query `up` retorna status dos targets configurados
- **AND** métricas dos serviços .NET são visíveis (ex: `http_server_request_duration_count`)

### Requirement: Loki para Logs
O sistema SHALL fornecer Loki para agregação e consulta de logs estruturados recebidos do OpenTelemetry Collector.

#### Scenario: Loki recebe logs do Collector
- **WHEN** OpenTelemetry Collector envia logs via OTLP HTTP para `http://loki:3100/otlp`
- **THEN** logs são armazenados no Loki com labels automáticos (service_name, level, deployment.environment)
- **AND** logs são consultáveis via LogQL
- **AND** logs são retidos por 7 dias

#### Scenario: Consulta de logs no Loki
- **WHEN** usuário consulta logs via Grafana Explore usando LogQL
- **THEN** query `{service_name="api"}` retorna logs do serviço API
- **AND** logs incluem traceId para correlação com traces
- **AND** logs estruturados preservam campos de contexto

### Requirement: Tempo para Traces
O sistema SHALL fornecer Tempo para armazenamento persistente e consulta de traces distribuídos recebidos do OpenTelemetry Collector.

#### Scenario: Tempo recebe traces do Collector
- **WHEN** OpenTelemetry Collector envia traces via OTLP gRPC para `tempo:4317`
- **THEN** traces são armazenados persistentemente no Tempo
- **AND** traces são consultáveis via interface do Grafana
- **AND** traces são retidos por 7 dias

#### Scenario: Consulta de traces no Tempo
- **WHEN** usuário consulta traces via Grafana Explore usando Tempo datasource
- **THEN** traces podem ser buscados por service_name, operation, tags
- **AND** spans são exibidos hierarquicamente
- **AND** correlação com logs é possível via traceId

### Requirement: Pyroscope para Profiling
O sistema SHALL fornecer Pyroscope para armazenamento e análise de dados de profiling de performance (CPU, memória) dos serviços.

#### Scenario: Pyroscope recebe dados de profiling
- **WHEN** serviços enviam dados de profiling para `http://pyroscope:4040`
- **THEN** dados são armazenados no Pyroscope
- **AND** dados são visualizáveis via interface web do Pyroscope
- **AND** dados são retidos por 7 dias

#### Scenario: Acesso ao Pyroscope
- **WHEN** usuário acessa `http://localhost:4040`
- **THEN** interface do Pyroscope é exibida
- **AND** dados de profiling são visualizáveis em formato de flamegraph
- **AND** integração com Grafana permite visualização unificada

### Requirement: Configuração Persistente
O sistema SHALL armazenar configurações e dados da stack de observabilidade de forma persistente usando volumes Docker.

#### Scenario: Dados persistem após reinicialização
- **WHEN** stack de observabilidade é parada e reiniciada
- **THEN** dados históricos de métricas, logs e traces são preservados
- **AND** configurações do Grafana (datasources, dashboards futuros) são preservadas
- **AND** volumes Docker mantêm dados entre reinicializações

### Requirement: Integração com Stack Existente
O sistema SHALL integrar-se com a infraestrutura existente sem quebrar funcionalidades atuais.

#### Scenario: Stack principal continua funcionando
- **WHEN** stack de observabilidade é adicionada
- **THEN** serviços .NET continuam enviando telemetria normalmente para o Collector
- **AND** OpenTelemetry Collector continua funcionando como antes
- **AND** não há impacto na performance dos serviços de aplicação
- **AND** stack principal pode ser iniciada independentemente da stack de observabilidade

