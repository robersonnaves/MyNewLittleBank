# PRD: Reescrita do Projeto MyLittleBank

**Autor:** Gemini  
**Versão:** 2.0  
**Data:** 2025-12-02

---

## 1. Introdução

### 1.1. Visão do Produto

O MyLittleBank é um sistema de processamento de transações bancárias simuladas. Ele foi projetado para ser uma plataforma robusta, escalável e resiliente, capaz de lidar com diferentes tipos de transações financeiras (Pix, transferência de dinheiro, cartão) de forma assíncrona.

### 1.2. Objetivo da Reescrita

O objetivo da reescrita é modernizar a base de código, otimizar a arquitetura para a nuvem e garantir maior manutenibilidade e observabilidade do sistema.

### 1.3. Escopo e Limitações

**Fora do Escopo (Versão Inicial):**

- Interface de usuário (Frontend)
- API Gateway para acesso externo
- Autenticação e autorização de usuários

---

## 2. Arquitetura do Sistema

### 2.1. Padrão Arquitetural

O sistema segue um padrão de **arquitetura de microsserviços orientada a eventos**.

### 2.2. Componentes de Infraestrutura

- **Orquestração:** Os serviços são containerizados com **Docker** e gerenciados pelo `docker-compose.yml`
- **Comunicação:** A comunicação entre os serviços é feita de forma assíncrona usando um message broker **RabbitMQ**
- **Persistência de Dados:** Os dados são armazenados em um banco de dados **PostgreSQL**
- **Stack Principal:** A lógica de negócio é implementada em **.NET**
- **Monitoramento/Busca:** Utiliza **OpenSearch** para logging e busca

### 2.3. Estrutura de Projetos

A solução é dividida em vários projetos C#, cada um com uma responsabilidade clara:

- **`Domain`**: Contém as entidades de negócio (`Client`, `BankAccount`, `Transaction`), interfaces e a lógica de domínio principal, agnóstica de infraestrutura
- **`Infra.Database`**: Implementa a camada de persistência usando **Entity Framework Core**. É responsável pelo `DbContext`, migrações e a implementação dos padrões **Repository** e **Unit of Work**
- **`Infra.Message`**: Responsável pela publicação de mensagens no RabbitMQ
- **`Services`**: Contém os microsserviços executáveis (workers), cada um rodando em seu próprio contêiner:
  - `PixTransactionReceiver`: Ouve a fila de transações Pix e as processa
  - `MoneyTransactionReceiver`: Ouve a fila de transferências de dinheiro e as processa
  - `HeartbeatPublisher/Consumer`: Serviços para monitoramento da saúde do sistema
- **`Mock.Transactions`**: Um worker para gerar e publicar transações falsas no sistema, utilizado para fins de teste e demonstração
- **`UseCases`**: Contém a lógica de aplicação que orquestra o domínio para executar uma tarefa específica (ex: `ProcessTransactions`)

---

## 3. Modelos de Dados e Persistência

### 3.1. Entidades Principais

- **`Client`**: Representa o cliente do banco. Um cliente pode ter várias contas
- **`BankAccount`**: Representa a conta bancária de um cliente. Uma conta pode ter várias transações
- **`Transaction`**: Classe base para transações financeiras

### 3.2. Estratégia de Persistência

- **Estratégia de Herança:** O sistema utiliza uma estratégia **Table-per-Hierarchy (TPH)** no Entity Framework. Todas as transações (`PixTransaction`, `MoneyTransaction`, `CardTransaction`) são armazenadas em uma única tabela `Transactions`, com uma coluna "discriminator" para diferenciar os tipos
- **Acesso a Dados:** O padrão **Unit of Work** é usado para agrupar operações de banco de dados em uma única transação, garantindo a consistência dos dados

---

## 4. Fluxos de Processamento

### 4.1. Fluxo de Processamento de Transações (Exemplo)

1. O `Mock.Transactions` (ou outro publicador) cria um DTO de transação (ex: `PixTransactionDTO`)
2. O `PublisherService` em `Infra.Message` serializa o DTO e o publica em uma exchange específica no **RabbitMQ**
3. O RabbitMQ roteia a mensagem para a fila apropriada (ex: `pix-transactions-queue`)
4. O worker `PixTransactionReceiver` recebe a mensagem da fila
5. O worker desserializa a mensagem, invoca o `ProcessTransactions` UseCase
6. O `UseCase` utiliza o `UnitOfWork` e os repositórios para buscar as entidades relevantes (contas), executar a lógica de negócio (debitar/creditar) e persistir o estado final no banco de dados **PostgreSQL**

---

## 5. Requisitos Funcionais

### 5.1. Gestão de Entidades

- **FR1:** O sistema deve permitir a gestão de Clientes e Contas Bancárias (CRUD)

### 5.2. Processamento de Transações

- **FR2:** O sistema deve ser capaz de processar 3 tipos de transações: **Pix**, **Transferência de Dinheiro** e **Cartão**
- **FR3:** Deve haver um mecanismo para publicar novas transações no sistema de forma assíncrona
- **FR4:** Cada tipo de transação deve ser processado por um serviço consumidor dedicado e independente

### 5.3. Monitoramento

- **FR5:** O sistema deve incluir um mecanismo de "heartbeat" para monitorar a saúde dos serviços ativos

---

## 6. Requisitos Não-Funcionais

### 6.1. Escalabilidade

- **NFR1:** Cada microsserviço deve ser escalável horizontalmente de forma independente

### 6.2. Resiliência

- **NFR2:** A falha em um serviço (ex: `CardTransactionReceiver`) não deve impactar o processamento de outros tipos de transação. O sistema deve implementar retentativas (`retries`) e filas de `dead-letter` para mensagens que falharam no processamento

### 6.3. Observabilidade

- **NFR3:** Implementar logging estruturado, tracing distribuído e métricas para todos os serviços, permitindo um monitoramento eficaz

### 6.4. Manutenibilidade

- **NFR4:** O código deve seguir os princípios de Clean Architecture, SOLID e ser bem documentado e testado (testes de unidade e integração)

### 6.5. Segurança

- **NFR5:** As conexões com o banco de dados e o message broker devem ser seguras. Dados sensíveis não devem ser expostos em logs

---

## 7. Stack Tecnológica

### 7.1. Stack Principal

A reescrita deve manter a stack tecnológica principal, atualizando para as versões mais recentes e estáveis:

- **Linguagem/Framework:** .NET 10 (versão mais recente LTS ou ST)
- **Containerização:** Docker
- **Banco de Dados:** PostgreSQL
- **Mensageria:** RabbitMQ
- **ORM:** Entity Framework Core
- **Observabilidade:** OpenTelemetry, com um backend como Jaeger/Prometheus/Grafana ou similar

### 7.2. Restrições Tecnológicas

Todas as mudanças mantêm as restrições: **sem AutoMapper, FluentAssertions ou MassTransit**.

---

## 8. Plano de Implementação

### 8.1. Fase 1: Modernização da Plataforma

#### 8.1.1. Upgrade de Plataforma (.NET 10)

- **Tarefa:** Atualizar `Directory.Build.props` para `net10.0` e habilitar `Nullable`, `ImplicitUsings`, `AnalysisLevel`
- **Critérios de Aceite:**
  - Solução compila com `dotnet build` sem warnings críticos
  - `Nullable` habilitado em todos os projetos de produção
  - Testes continuam executando
- **Impactos:** Possível ajuste em APIs obsoletas; validação de compatibilidade de pacotes (EF Core, RabbitMQ.Client, Serilog, OTel)

#### 8.1.2. Adoção de C#/.NET 10

- **Desempenho:** `System.Text.Json` com contexts gerados; uso de `ValueTask` e buffers (`ReadOnlyMemory<byte>`) em hot paths
- **Coleções:** `FrozenDictionary`/`ImmutableArray` para configurações estáticas de roteamento
- **Encerramento gracioso:** Cancelamento e shutdown via `IHostedService` com `CancellationToken` em toda a cadeia

### 8.2. Fase 2: Mensageria Confiável

#### 8.2.1. Infra.Message com RabbitMQ.Client

- **Tarefa:** Criar fábrica de conexão (`RabbitConnectionFactory`), `PublisherService` com publisher confirms e `ConsumerService` com ack manual
- **Retries/DLQ:** Implementar retentativas com filas de atraso (TTL) e DLQ por fila usando `x-dead-letter-exchange`/`routing-key`
- **Idempotência:** Propagar `MessageId`/`traceparent` em headers; inbox para deduplicação nos consumidores
- **Critérios de Aceite:**
  - Publicações confirmadas
  - Consumidores com reprocessamento controlado
  - DLQ funcionando
  - Métricas de retries disponíveis
- **Impactos:** Padronização de exchanges/queues por tipo; redução de acoplamento com abstrações leves

### 8.3. Fase 3: Confiabilidade e Consistência

#### 8.3.1. Outbox/Inbox em Banco

- **Tarefa:** Implementar Outbox em `Infra.Database` (+ dispatcher em background) e Inbox nos consumidores
- **Outbox:** Registrar eventos/mensagens no commit da transação; dispatcher lê e publica de forma confiável
- **Inbox:** Registrar processamento por `MessageId` para garantir idempotência
- **Critérios de Aceite:**
  - Nenhuma perda/duplicação de mensagens em cenários de falha
  - Reconciliação consistente entre DB e fila
- **Impactos:** Maior consistência eventual; pequena latência adicional controlada

### 8.4. Fase 4: Observabilidade

#### 8.4.1. Instrumentação com OpenTelemetry

- **Tarefa:** Instrumentar `Services/*` com OpenTelemetry (traces, métricas, logs) e propagação de contexto
- **Propagação:** Inserir/extrair `traceparent` nos headers RabbitMQ; correlação com operações de banco
- **Export:** OTLP para Jaeger/Tempo e Prometheus; logs estruturados com Serilog para OpenSearch
- **Critérios de Aceite:**
  - Traces de ponta-a-ponta visíveis
  - Métricas por tipo de transação e retries
  - Logs correlacionados sem PII
- **Impactos:** Melhor diagnóstico e SLOs; custo operacional dos backends de observabilidade

### 8.5. Fase 5: Qualidade e Testes

#### 8.5.1. Testes de Integração — RabbitMQ/Postgres

- **Tarefa:** Base de testes em `Tests/IntegrationTest` usando `docker-compose` dos serviços de infraestrutura
- **Cobertura:** Publicação/consumo, outbox dispatch, inbox dedupe, persistência EF, métricas/trace básicos
- **Critérios de Aceite:**
  - Pipeline de CI executa testes de integração com containers
  - Relatórios estáveis
- **Impactos:** Tempo adicional de execução na CI; maior confiança de mudanças

### 8.6. Padrões Complementares

#### 8.6.1. Configuração e Saúde

- **Configuração forte:** `Options<T>` centralizados em `Shared/Configuration` com validação no startup
- **Health Checks:** `Microsoft.Extensions.Diagnostics.HealthChecks` para RabbitMQ, Postgres e workers
- **Segurança:** Segredos via variáveis de ambiente/User Secrets; TLS em brokers/DB; redaction de logs

#### 8.6.2. DevEx, Docker e CI/CD

- **Props unificados:** Centralizar versões e propriedades em `Directory.Build.props` para reduzir duplicação
- **Docker:** Builds multi-stage por serviço, healthchecks e imagens enxutas (AOT quando viável)
- **Compose:** Redes nomeadas, dependências explícitas, healthchecks e limites de recursos; perfis para local/CI
- **CI:** GitHub Actions com jobs de build/test, integração (subindo serviços), e build/push de imagens; versionamento semântico
- **DevContainer:** `.devcontainer` para ambiente padronizado com acesso aos serviços

---

## 9. Riscos e Mitigações

### 9.1. Riscos Técnicos

- **Risco:** Incompatibilidade de pacotes com .NET 10
  - **Mitigação:** Matriz de compatibilidade e versões pinadas em `Directory.Build.props`
- **Risco:** Complexidade do Outbox/Inbox
  - **Mitigação:** Feature flags, migração faseada e observabilidade reforçada
- **Risco:** Overhead de OTel/Serilog
  - **Mitigação:** Amostragem e níveis de log controlados por ambiente

---

## 10. Cronograma e Entregáveis

### 10.1. Cronograma por Semana

- **Semana 1:** Upgrade `net10.0` + ajustes de compilação; criação de `Infra.Message` básica (publisher/consumer)
- **Semana 2:** Retries/DLQ e padronização de topologia; início do Outbox e dispatcher
- **Semana 3:** Inbox nos consumidores e instrumentação OTel; métricas e logs
- **Semana 4:** Testes de integração completos; documentação e exemplos de operação

### 10.2. Próximos Passos Concretos

1. Atualizar `Directory.Build.props` para `net10.0`, habilitar `Nullable`, `ImplicitUsings`, `AnalysisLevel`
2. Refatorar `Infra.Message`: fábrica de conexão RabbitMQ, `PublisherService` com confirms e `ConsumerService` com ack manual, retries e DLQ
3. Implementar Outbox em `Infra.Database` e um dispatcher de background; adicionar Inbox nos consumidores
4. Instrumentar `Services/*` com OpenTelemetry e propagação de contexto
5. Criar base de testes de integração com docker-compose para RabbitMQ/Postgres

---

## 11. Anexos

### 11.1. Glossário

- **TPH (Table-per-Hierarchy):** Estratégia de mapeamento do Entity Framework onde múltiplas classes herdeiras são armazenadas em uma única tabela
- **DLQ (Dead Letter Queue):** Fila para mensagens que falharam no processamento após múltiplas tentativas
- **OTel (OpenTelemetry):** Padrão aberto para instrumentação, geração, coleta e exportação de telemetria
- **OTLP (OpenTelemetry Protocol):** Protocolo de transporte para dados de telemetria
- **PII (Personally Identifiable Information):** Informações pessoais identificáveis

### 11.2. Referências

- Documentação oficial do .NET 10
- Documentação do RabbitMQ
- Especificação do OpenTelemetry
- Padrões de Clean Architecture e SOLID
