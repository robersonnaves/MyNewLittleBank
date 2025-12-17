# Relatório de Cobertura de Testes - MyNewLittleBank

**Data da Análise:** 17 de dezembro de 2025  
**Total de Arquivos de Código:** 107 arquivos  
**Total de Arquivos de Teste:** 16 arquivos  
**Proporção:** ~15% de arquivos de teste

---

## 📊 Visão Geral

### Estatísticas Gerais

| Camada | Arquivos de Código | Arquivos de Teste | Cobertura Estimada |
|--------|-------------------|-------------------|-------------------|
| **Domain** | 24 | 3 | 🟡 **Parcial (40%)** |
| **UseCases** | 9 | 2 | 🟢 **Boa (60%)** |
| **Infra.Database** | 19 | 2 | 🟡 **Parcial (30%)** |
| **Infra.Message** | 8 | 0 | 🔴 **Nenhuma (0%)** |
| **API** | 6 | 0 | 🔴 **Nenhuma (0%)** |
| **Services.Card** | 4 | 0 | 🔴 **Nenhuma (0%)** |
| **Services.Money** | 4 | 0 | 🔴 **Nenhuma (0%)** |
| **Services.Pix** | 4 | 0 | 🔴 **Nenhuma (0%)** |
| **Services.Heartbeats** | 6 | 0 | 🔴 **Nenhuma (0%)** |
| **Mock.Transactions** | 9 | 1 | 🟡 **Parcial (20%)** |
| **Shared** | 3 | 0 | 🔴 **Nenhuma (0%)** |
| **Integration** | - | 5 | 🟢 **Testes E2E** |

### Legenda de Cobertura
- 🟢 **Boa (≥50%)**: Cobertura satisfatória
- 🟡 **Parcial (20-49%)**: Cobertura básica, necessita expansão
- 🔴 **Nenhuma/Crítica (<20%)**: Sem testes ou cobertura insuficiente

---

## ✅ Testes Existentes

### 1. Domain.Tests (3 arquivos)

#### ✅ **BankAccountTests.cs**
- ✅ `DebitShouldReturnFailureWhenBalanceInsufficient()` - Testa débito com saldo insuficiente
- ✅ `CreditShouldIncreaseBalance()` - Testa crédito aumentando saldo

**Cobertura:** Operações básicas de conta bancária (débito/crédito)

#### ✅ **ValueObjectsTests.cs**
- ✅ `CpfShouldFailWhenChecksumInvalid()` - Validação de CPF inválido
- ✅ `CpfShouldSucceedForValidCpf()` - Validação de CPF válido
- ✅ `MoneyShouldFailWhenNegative()` - Validação de valor negativo
- ✅ `MoneyShouldSupportArithmetic()` - Operações aritméticas com Money

**Cobertura:** Value Objects principais (CPF e Money)

#### ✅ **TransactionDtoMapperTests.cs**
**Cobertura:** Mapeamento de DTOs de transação

### 2. UseCases.Tests (2 arquivos)

#### ✅ **ProcessTransactionsHandlerTests.cs**
- ✅ `HandleAsync_PixTransaction_Should_Debit_Account_And_Write_Outbox()` - Fluxo completo de transação PIX
- ✅ `HandleAsync_CardTransaction_Should_Return_Failure_When_Insufficient_Funds()` - Transação cartão com saldo insuficiente
- ✅ `HandleAsync_MoneyTransaction_Should_Fail_When_Account_Not_Found()` - Transação money com conta inexistente
- ✅ `HandleAsync_CardTransaction_Should_Return_Failure_When_Notification_Fails()` - Falha na notificação

**Cobertura:** Handler de processamento de transações com cenários de sucesso e falha

#### ✅ **CoreApiUseCasesTests.cs**
- ✅ `CreateClient_Should_Persist_When_Data_Valid()` - Criação de cliente válido
- ✅ `CreateClient_Should_Return_Failure_When_Cpf_Already_Exists()` - Duplicação de CPF
- ✅ `GetClient_Should_Return_NotFound_When_Client_Does_Not_Exist()` - Cliente não encontrado
- ✅ `GetAccountBalance_Should_Return_Current_Balance_When_Account_Exists()` - Consulta de saldo
- ✅ `GetAccount_Should_Return_NotFound_When_Account_Missing()` - Conta não encontrada

**Cobertura:** Casos de uso principais da API (clientes e contas)

### 3. Infra.Database.Tests (2 arquivos)

#### ✅ **MyNewLittleBankContextTests.cs**
- ✅ `SaveChangesAsyncPersistsEntitiesWithDiscriminator()` - Persistência com discriminator (TPH)
- ✅ `ModelShouldExposeDiscriminatorAndConcurrencyTokens()` - Validação do modelo EF
- ✅ `RepositoryShouldApplySpecifications()` - Aplicação de specifications

**Cobertura:** Configuração do DbContext e padrão Repository/Specification

#### ✅ **OutboxInboxTests.cs**
- ✅ Testes do padrão Outbox/Inbox

### 4. Integration Tests (5 arquivos)

#### ✅ **RebusEndToEndTests.cs**
- ✅ `Publish_and_consume_should_reach_handler()` - Fluxo completo de mensageria
- ✅ `Failing_handler_should_retry_and_eventually_give_up()` - Retry logic do Rebus
- ✅ `Outbox_dispatcher_should_publish_and_consumer_should_receive()` - Integração Outbox → RabbitMQ
- ✅ `Publish_should_propagate_traceparent_header()` - Propagação de trace context (distributed tracing)

**Cobertura:** Integração end-to-end com RabbitMQ e Rebus

#### ✅ **OutboxInboxTests.cs (Integration)**
- ✅ `OutboxDispatcher_should_mark_messages_sent_and_publish_payloads()` - Dispatcher do Outbox
- ✅ `InboxMessageStore_should_reject_duplicate_processing()` - Idempotência do Inbox

**Cobertura:** Padrões transacionais Outbox/Inbox

#### ✅ **InfrastructureSmokeTests.cs**
**Cobertura:** Smoke tests de infraestrutura

### 5. Mock.Transactions.Tests (1 arquivo)

#### ✅ **TransactionDtoGeneratorFactoryTests.cs**
**Cobertura:** Geração de dados mock para transações

---

## 🔴 Lacunas Críticas de Cobertura

### 1. **API (Endpoints) - 0% de Cobertura** 🔴

#### Arquivos sem Testes:
- `src/API/Endpoints/ClientEndpoints.cs`
- `src/API/Endpoints/AccountEndpoints.cs`
- `src/API/Program.cs`

#### Testes Sugeridos:

**ClientEndpoints.cs:**
```csharp
// Arquivo sugerido: tests/API.Tests/ClientEndpointsTests.cs

[Fact]
public async Task POST_Clients_Should_Return_201_When_Data_Valid()
{
    // Arrange
    var client = new HttpClient(new WebApplicationFactory<Program>());
    var request = new CreateClientRequest("52998224725", "Maria", "maria@test.com", "11999999999");
    
    // Act
    var response = await client.PostAsJsonAsync("/clients", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}

[Fact]
public async Task POST_Clients_Should_Return_400_When_Cpf_Invalid()
{
    // Testa validação de CPF no endpoint
}

[Fact]
public async Task GET_Clients_ById_Should_Return_404_When_Not_Found()
{
    // Testa endpoint GET retornando 404
}

[Fact]
public async Task GET_Clients_ByCpf_Should_Return_200_With_Client_Data()
{
    // Testa endpoint GET por CPF
}
```

**AccountEndpoints.cs:**
```csharp
// Arquivo sugerido: tests/API.Tests/AccountEndpointsTests.cs

[Fact]
public async Task POST_Accounts_Should_Return_201_When_Client_Exists()
{
    // Testa criação de conta para cliente existente
}

[Fact]
public async Task GET_Accounts_ById_Should_Return_200_With_Balance()
{
    // Testa consulta de conta com saldo
}

[Fact]
public async Task GET_Accounts_Should_Return_404_When_Account_Not_Found()
{
    // Testa endpoint GET retornando 404
}
```

### 2. **Infra.Message (Messaging Infrastructure) - 0% de Cobertura** 🔴

#### Arquivos sem Testes:
- `src/Infra.Message/TracingIncomingStep.cs`
- `src/Infra.Message/TracingHeadersStep.cs`
- `src/Infra.Message/RebusMessagingBus.cs`
- `src/Infra.Message/RebusMessagePublisher.cs`
- `src/Infra.Message/MessageEnvelopeHandler.cs`

**Nota:** Estes componentes têm cobertura INDIRETA via testes de integração (`RebusEndToEndTests`), mas faltam testes unitários específicos.

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/Infra.Message.Tests/TracingIncomingStepTests.cs

[Fact]
public async Task Process_Should_Create_Activity_With_Traceparent()
{
    // Testa criação de Activity com propagação de trace context
}

[Fact]
public async Task Process_Should_Continue_Trace_When_Traceparent_Present()
{
    // Testa continuação de trace distribuído
}
```

```csharp
// Arquivo sugerido: tests/Infra.Message.Tests/MessageEnvelopeHandlerTests.cs

[Fact]
public async Task Handle_Should_Invoke_Correct_Consumer()
{
    // Testa roteamento para consumer correto
}

[Fact]
public async Task Handle_Should_Skip_When_No_Consumer_Can_Handle()
{
    // Testa comportamento quando nenhum consumer pode processar
}
```

### 3. **Services (Receivers) - 0% de Cobertura** 🔴

#### Arquivos sem Testes:
- `src/Services.Card/CardTransactionReceiver.cs`
- `src/Services.Money/MoneyTransactionReceiver.cs`
- `src/Services.Pix/PixTransactionReceiver.cs`
- `src/Services.Heartbeats/HeartbeatConsumer.cs`

**Nota Importante:** O `ProcessTransactionsHandler` (usado por todos os receivers de transação) tem boa cobertura, mas os **receivers em si** não têm testes.

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/Services.Card.Tests/CardTransactionReceiverTests.cs

[Fact]
public async Task HandleAsync_Should_Process_Valid_Card_Transaction()
{
    // Arrange
    var handler = new Mock<IHandler<CardTransactionDto>>();
    var receiver = new CardTransactionReceiver(handler.Object);
    var envelope = new MessageEnvelope("card.transaction", """{"id":"..."}""", "card");
    
    // Act
    await receiver.HandleAsync(envelope, CancellationToken.None);
    
    // Assert
    handler.Verify(h => h.HandleAsync(It.IsAny<CardTransactionDto>(), It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task HandleAsync_Should_Log_Error_On_Deserialization_Failure()
{
    // Testa tratamento de JSON inválido
}

[Fact]
public void CanHandle_Should_Return_True_For_Card_Routing_Key()
{
    // Testa lógica de roteamento
}
```

**Aplicar padrão similar para:**
- `MoneyTransactionReceiverTests.cs`
- `PixTransactionReceiverTests.cs`
- `HeartbeatConsumerTests.cs`

### 4. **Domain Entities - Cobertura Parcial** 🟡

#### Entidades com Testes Limitados:
- ✅ `BankAccount` - Cobertura **básica** (débito/crédito)
- ❌ `Client` - **Sem testes unitários**
- ❌ `Transaction` (base) - **Sem testes unitários**
- ❌ `CardTransaction` - **Sem testes unitários**
- ❌ `MoneyTransaction` - **Sem testes unitários**
- ❌ `PixTransaction` - **Sem testes unitários**

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/Domain.Tests/ClientTests.cs

[Fact]
public void Create_Should_Fail_When_Name_Is_Empty()
{
    var result = Client.Create(clientId, validCpf, "", "email@test.com", "11999999999");
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be("client_name_empty");
}

[Fact]
public void Update_Should_Update_Client_Information()
{
    // Testa método Update do Client
}

[Fact]
public void AddAccount_Should_Fail_When_Account_Does_Not_Belong_To_Client()
{
    // Testa validação de propriedade da conta
}
```

```csharp
// Arquivo sugerido: tests/Domain.Tests/TransactionTests.cs

[Fact]
public void CardTransaction_Create_Should_Succeed_With_Valid_Data()
{
    var result = CardTransaction.Create(transactionId, clientId, accountNumber, amount, "4111111111111111");
    result.IsSuccess.Should().BeTrue();
}

[Fact]
public void PixTransaction_Should_Require_Both_Pix_Keys()
{
    // Testa validação de chaves PIX obrigatórias
}

[Fact]
public void Transaction_Status_Transitions_Should_Be_Valid()
{
    // Testa transições de status válidas
}
```

```csharp
// Arquivo sugerido: tests/Domain.Tests/BankAccountAdvancedTests.cs

[Fact]
public void Debit_Should_Prevent_Concurrent_Modifications()
{
    // Testa controle de concorrência (xmin)
}

[Fact]
public void Credit_Should_Reject_Zero_Amount()
{
    // Testa validação de valores
}

[Fact]
public void Open_Should_Fail_With_Negative_Initial_Balance()
{
    // Testa validação na criação
}
```

### 5. **Value Objects - Cobertura Parcial** 🟡

#### Value Objects com Testes:
- ✅ `Cpf` - Cobertura básica
- ✅ `Money` - Cobertura básica
- ❌ `AccountNumber` - **Sem testes**
- ❌ `ClientId` - **Sem testes**
- ❌ `TransactionId` - **Sem testes**

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/Domain.Tests/ValueObjectsAdvancedTests.cs

[Fact]
public void AccountNumber_Should_Fail_When_Format_Invalid()
{
    var result = AccountNumber.TryCreate("123"); // muito curto
    result.IsFailure.Should().BeTrue();
}

[Fact]
public void ClientId_Should_Fail_When_Empty_Guid()
{
    var result = ClientId.TryCreate(Guid.Empty);
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be("client_id_empty");
}

[Fact]
public void TransactionId_Should_Generate_Unique_Values()
{
    var id1 = TransactionId.New();
    var id2 = TransactionId.New();
    id1.Value!.Value.Should().NotBe(id2.Value!.Value);
}

[Fact]
public void Money_Should_Support_Multiplication_And_Division()
{
    // Testa operações avançadas com Money
}

[Fact]
public void Cpf_Should_Normalize_Input()
{
    var result = Cpf.TryCreate("529.982.247-25"); // com formatação
    result.IsSuccess.Should().BeTrue();
    result.Value.Value.Should().Be("52998224725"); // sem formatação
}
```

### 6. **UseCases - Handlers Faltando** 🟡

#### Handlers sem Testes:
- ❌ `OpenAccountHandler` - **Sem testes**
- ❌ `GetClientByCpfHandler` - **Sem testes**

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/UseCases.Tests/AccountUseCasesTests.cs

[Fact]
public async Task OpenAccount_Should_Create_Account_With_Initial_Balance()
{
    // Testa criação de conta com saldo inicial
}

[Fact]
public async Task OpenAccount_Should_Fail_When_Client_Does_Not_Exist()
{
    // Testa validação de cliente existente
}

[Fact]
public async Task GetClientByCpf_Should_Return_Client_When_Found()
{
    // Testa busca por CPF
}

[Fact]
public async Task GetClientByCpf_Should_Return_NotFound_When_Cpf_Not_Registered()
{
    // Testa CPF não cadastrado
}
```

### 7. **Infra.Database - Repositories e Outbox/Inbox** 🟡

#### Componentes com Cobertura Limitada:
- ✅ `EfRepository` - Cobertura básica via `MyNewLittleBankContextTests`
- ✅ `OutboxDispatcher` - Cobertura via testes de integração
- ✅ `InboxMessageStore` - Cobertura via testes de integração
- ❌ `OutboxWriter` - **Sem testes diretos**
- ❌ `UnitOfWork` - **Sem testes diretos**

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/Infra.Database.Tests/OutboxWriterTests.cs

[Fact]
public async Task AddAsync_Should_Insert_Outbox_Message_With_Pending_Status()
{
    // Testa inserção de mensagem no outbox
}

[Fact]
public async Task AddAsync_Should_Serialize_Payload_Correctly()
{
    // Testa serialização do payload
}
```

```csharp
// Arquivo sugerido: tests/Infra.Database.Tests/UnitOfWorkTests.cs

[Fact]
public async Task SaveChangesAsync_Should_Commit_Transaction()
{
    // Testa commit de transação
}

[Fact]
public async Task SaveChangesAsync_Should_Return_Affected_Rows_Count()
{
    // Testa retorno de linhas afetadas
}
```

### 8. **Shared (Observability & Health) - 0% de Cobertura** 🔴

#### Arquivos sem Testes:
- `src/Shared/Observability/ApplicationMetrics.cs`
- `src/Shared/Observability/ObservabilityExtensions.cs`
- `src/Shared/Health/HealthCheckExtensions.cs`

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/Shared.Tests/ApplicationMetricsTests.cs

[Fact]
public void RecordTransactionProcessed_Should_Increment_Counter()
{
    // Testa métrica de transações processadas
}

[Fact]
public void RecordTransactionFailed_Should_Tag_With_Error_Type()
{
    // Testa métricas de falhas com tags
}
```

```csharp
// Arquivo sugerido: tests/Shared.Tests/HealthCheckTests.cs

[Fact]
public async Task DatabaseHealthCheck_Should_Return_Healthy_When_Connected()
{
    // Testa health check do banco
}

[Fact]
public async Task RabbitMqHealthCheck_Should_Return_Unhealthy_When_Disconnected()
{
    // Testa health check do RabbitMQ
}
```

### 9. **Notifications (UseCases) - 0% de Cobertura** 🔴

#### Arquivos sem Testes:
- `src/UseCases/Notifications/HttpNotificationSender.cs`

#### Testes Sugeridos:

```csharp
// Arquivo sugerido: tests/UseCases.Tests/NotificationTests.cs

[Fact]
public async Task NotifyInsufficientFundsAsync_Should_Send_HTTP_Request()
{
    // Testa envio de notificação via HTTP
}

[Fact]
public async Task NotifyInsufficientFundsAsync_Should_Return_Failure_On_Network_Error()
{
    // Testa tratamento de erro de rede
}

[Fact]
public async Task NotifyInsufficientFundsAsync_Should_Retry_On_Transient_Failures()
{
    // Testa lógica de retry
}
```

---

## 📋 Priorização de Testes

### 🔥 Prioridade CRÍTICA (Implementar IMEDIATAMENTE)

1. **API Endpoints** - Garantir que contratos HTTP funcionam
2. **ProcessTransactionsHandler cenário de erro encontrado** - Adicionar teste específico para o bug do `ClientId.Value`
3. **Services Receivers** - Garantir desserialização e roteamento corretos

### ⚠️ Prioridade ALTA (Implementar em BREVE)

4. **Domain Entities completas** - Cobrir todas as regras de negócio
5. **Value Objects restantes** - Validações são críticas
6. **UseCases Handlers faltantes** - Completar cobertura de casos de uso

### 📌 Prioridade MÉDIA (Implementar quando possível)

7. **Infra.Message unitários** - Já tem cobertura via integração, mas unitários ajudam no debug
8. **Outbox/Inbox diretos** - Já tem cobertura via integração
9. **Observability & Health** - Importante mas menos crítico

### 📊 Prioridade BAIXA (Implementar para completude)

10. **Mock.Transactions** - É código de teste/mock, menos crítico
11. **Configurations do EF** - Cobertas indiretamente pelos testes de contexto

---

## 🎯 Objetivos de Cobertura Recomendados

### Meta de Curto Prazo (1-2 semanas)
- **Domain**: 80% de cobertura
- **UseCases**: 90% de cobertura
- **API**: 70% de cobertura
- **Services**: 60% de cobertura

### Meta de Médio Prazo (1 mês)
- **Infra.Database**: 70% de cobertura
- **Infra.Message**: 60% de cobertura
- **Shared**: 50% de cobertura

### Meta de Longo Prazo (2-3 meses)
- **Cobertura Geral**: ≥75% em todo o projeto
- **Camadas Críticas** (Domain, UseCases): ≥85%

---

## 🛠️ Ferramentas Recomendadas

### Medição de Cobertura
```bash
# Instalar ferramenta de cobertura
dotnet tool install --global dotnet-coverage

# Executar testes com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Gerar relatório HTML
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Configuração no CI/CD
```yaml
# Exemplo para GitHub Actions
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

- name: Generate coverage report
  run: reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" -targetdir:"./coverage-report"

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
```

---

## 📚 Padrões de Teste Recomendados

### Estrutura de Teste (AAA Pattern)
```csharp
[Fact]
public async Task MethodName_Should_ExpectedBehavior_When_Condition()
{
    // Arrange - Preparação
    var dependency = new Mock<IDependency>();
    var sut = new SystemUnderTest(dependency.Object);
    
    // Act - Execução
    var result = await sut.MethodAsync(parameters);
    
    // Assert - Verificação
    result.IsSuccess.Should().BeTrue();
    dependency.Verify(d => d.Method(), Times.Once);
}
```

### Testes de Integração
```csharp
[Trait("Category", "Integration")]
[Fact]
public async Task Integration_Test_Name()
{
    // Usa fixtures compartilhadas (IClassFixture<IntegrationInfrastructureFixture>)
    // Testa componentes integrados
}
```

### Fake Repositories
- ✅ **BOM:** Usar fakes in-memory para testes unitários (como mostrado em `ProcessTransactionsHandlerTests`)
- ❌ **EVITAR:** Mock excessivo de repositórios em testes unitários
- ✅ **BOM:** Usar banco real (Testcontainers) para testes de integração

---

## 🚀 Próximos Passos

### Semana 1: Testes Críticos
1. ✅ Adicionar teste para bug do `ClientId.Value` em `ProcessTransactionsHandlerTests`
2. ✅ Criar `tests/API.Tests/` com testes de endpoints
3. ✅ Criar `tests/Services.Card.Tests/`, `Services.Money.Tests/`, `Services.Pix.Tests/`

### Semana 2: Domain Completo
4. ✅ Expandir `BankAccountTests` com cenários avançados
5. ✅ Criar `ClientTests` com todos os métodos
6. ✅ Criar `TransactionTests` para todas as subclasses
7. ✅ Completar `ValueObjectsAdvancedTests`

### Semana 3: UseCases e Infra
8. ✅ Criar `AccountUseCasesTests` para handlers faltantes
9. ✅ Criar `NotificationTests`
10. ✅ Adicionar testes unitários para `Infra.Message` components

### Semana 4: Observabilidade e Refinamento
11. ✅ Criar `Shared.Tests` para métricas e health checks
12. ✅ Configurar ferramentas de cobertura no CI/CD
13. ✅ Revisar e refatorar testes existentes

---

## 📈 Métricas de Sucesso

Após implementação completa do plano:

1. **Cobertura de Código**: ≥75% geral, ≥85% em Domain e UseCases
2. **Build Verde**: 100% dos testes passando no CI/CD
3. **Tempo de Execução**: Testes unitários <30s, integração <2min
4. **Confiabilidade**: Zero flaky tests
5. **Documentação**: Cada teste documenta claramente o comportamento esperado

---

**Preparado por:** Análise Automatizada de Testes  
**Próxima Revisão:** Após implementação dos testes críticos  
**Responsável pela Implementação:** Equipe de Desenvolvimento
