## 1. Scaffolding e Configuração Inicial
- [x] 1.1 Criar estrutura de diretórios `workers/notification-receiver-go/` com `cmd/server/`, `internal/handler/`, `internal/storage/`, `internal/telemetry/`
- [x] 1.2 Inicializar módulo Go (`go mod init`) e adicionar dependências iniciais (chi router, OpenTelemetry SDK para Go)
- [x] 1.3 Criar `README.md` com instruções de build local e variáveis de ambiente

## 2. Implementação Core do Worker
- [x] 2.1 Implementar `internal/telemetry/otel.go`: setup de OpenTelemetry SDK (tracer provider, meter provider, OTLP gRPC exporter para Collector)
- [x] 2.2 Implementar `internal/storage/filewriter.go`: função para persistir JSON em arquivo com naming pattern `{transactionId}.json` COM verificação de idempotência (verificar se arquivo já existe antes de escrever; se existir, retornar sucesso sem reescrever)
- [x] 2.3 Implementar struct para payload de notificação (matching `InsufficientFundsNotification` do .NET) com tags JSON
- [x] 2.4 Implementar `internal/handler/alerts.go`: handler HTTP para `POST /alerts/insufficient-funds` com validação de payload, extração de trace context do header `X-Correlation-Id`, spans OpenTelemetry e chamada ao filewriter (tratando caso de duplicata como sucesso)
- [x] 2.5 Implementar endpoint `/health` para health checks
- [x] 2.6 Implementar `cmd/server/main.go`: entrypoint que configura chi router, middlewares (logging, recovery, OpenTelemetry), graceful shutdown (context com SIGTERM/SIGINT), lê env vars e inicia servidor HTTP

## 3. Observabilidade e Logs
- [x] 3.1 Configurar `log/slog` com handler JSON incluindo trace/span IDs automaticamente via context
- [x] 3.2 Implementar métricas OpenTelemetry: contador `notifications_received_total` (labels: status=success|duplicate|invalid_payload|storage_error), histograma `notification_processing_duration_seconds`
- [x] 3.3 Adicionar log statements para eventos-chave: startup com config, requisições recebidas (nível Info), duplicatas detectadas (nível Info), erros de validação/persistência (nível Warn/Error), shutdown

## 4. Containerização
- [x] 4.1 Criar `Dockerfile` multi-stage: stage 1 com `golang:1.22-alpine` para build, stage 2 com `alpine:latest` ou `scratch` copiando binário estático
- [x] 4.2 Adicionar entrada no `infra/docker-compose.yml` para serviço `notification-receiver` com build context apontando para `../workers/notification-receiver-go`, env vars (`PORT=8080`, `NOTIFICATIONS_DIR=/data/notifications`, `OTEL_EXPORTER_OTLP_ENDPOINT=otel-collector:4317`, `OTEL_SERVICE_NAME=notification-receiver`), named volume `notification_data:/data/notifications`, network `bank-net`, health check (`curl -f http://localhost:8080/health`), depends_on `otel-collector`
- [x] 4.3 Declarar volume `notification_data` na seção `volumes` do docker-compose
- [x] 4.4 Testar build da imagem Docker e startup do container isoladamente

## 5. Testes Unitários e Integração
- [ ] 5.1 Escrever testes unitários para `internal/storage/filewriter.go` (sucesso primeira escrita, duplicata idempotente retorna sucesso sem reescrever, erro de I/O, diretório criado automaticamente)
- [ ] 5.2 Escrever testes unitários para `internal/handler/alerts.go` (payload válido primeira vez retorna 202, payload duplicado retorna 202, payload inválido retorna 400, mock do storage para testar erro de persistência retorna 500)
- [ ] 5.3 Escrever teste de integração end-to-end: subir worker localmente ou em container de teste, enviar requisição HTTP com trace context, verificar arquivo JSON criado, enviar mesma requisição novamente (retry) e verificar que arquivo não foi modificado e resposta ainda é 202
- [ ] 5.4 Verificar que métricas `notifications_received_total{status="duplicate"}` incrementam corretamente em retries
- [ ] 5.5 Executar todos os testes com `go test ./...` e garantir cobertura > 80%

## 6. Integração com Stack .NET e Validação End-to-End
- [ ] 6.1 Atualizar `appsettings.json` dos workers .NET (`Services.Card`, `Services.Money`, `Services.Pix`) para apontar `Notifications:BaseUrl` para `http://notification-receiver:8080` (ou variável de ambiente no docker-compose)
- [ ] 6.2 Subir stack completa com `docker-compose up` (incluindo novo worker Go)
- [ ] 6.3 Provocar transação de teste com saldo insuficiente (ex: via Mock.Transactions ou chamada direta à API)
- [ ] 6.4 Verificar no Jaeger se trace end-to-end aparece: API → Worker .NET → Notification Receiver (Go)
- [ ] 6.5 Verificar no Prometheus se métricas `notifications_received_total{status="success"}` e `notification_processing_duration_seconds` aparecem com namespace `mynewlittlebank`
- [ ] 6.6 Verificar arquivo JSON criado em `notification_data` volume com nome `{transactionId}.json` (acessar via `docker volume inspect` ou bind mount temporário) e validar conteúdo
- [ ] 6.7 Provocar retry da mesma transação (se possível via replay ou forçando re-envio) e verificar que métrica `notifications_received_total{status="duplicate"}` incrementa e arquivo não é modificado

## 7. Documentação e Validação OpenSpec
- [ ] 7.1 Atualizar `doc/AGENTS.md` ou criar `workers/notification-receiver-go/ARCHITECTURE.md` descrevendo fluxo, configuração e troubleshooting
- [ ] 7.2 Adicionar comentários inline no código Go (godoc style) para funções públicas
- [ ] 7.3 Rodar `openspec validate add-notification-receiver-worker --strict` e corrigir qualquer issue reportado
- [ ] 7.4 Solicitar revisão da proposta e aprovação antes de merge
