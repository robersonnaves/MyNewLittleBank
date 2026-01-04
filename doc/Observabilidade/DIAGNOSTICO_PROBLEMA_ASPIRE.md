# Diagnóstico: Problema com Aspire Dashboard

## 🔍 Resumo Executivo

O Aspire Dashboard **não é compatível** com o OpenTelemetry Collector como intermediário na configuração atual. Após extensa análise, identificamos incompatibilidades no protocolo gRPC/HTTP que impedem o funcionamento correto do fluxo de dados.

## 🐛 Problema Original

```
Aplicações .NET → OTel Collector → Aspire Dashboard
                  (funciona)      (FALHA)
```

**Sintoma**: Dados chegam ao Collector mas não aparecem no Aspire Dashboard.

## 📋 Histórico de Tentativas

### Tentativa 1: gRPC (porta 18888)
```yaml
endpoint: apphost:18888
```
**Erro**: `rpc error: code = Unavailable desc = connection error: desc = "initial http2 frame from server is not a settings frame: *http2.GoAwayFrame"`

**Análise**: O Aspire Dashboard gRPC endpoint não responde corretamente ao handshake HTTP/2 do Collector.

### Tentativa 2: HTTP com Protobuf (porta 18890)
```yaml
endpoint: http://apphost:18890
```
**Erro**: `HTTP Status Code 500` - `Protocol message contained a tag with an invalid wire type`

**Análise**: O endpoint HTTP recebeu dados, mas não conseguiu decodificar o Protobuf enviado pelo Collector.

### Tentativa 3: HTTP com JSON (porta 18890)
```yaml
endpoint: http://apphost:18890
encoding: json
```
**Erro**: `HTTP Status Code 415` - Unsupported Media Type

**Análise**: O Aspire Dashboard HTTP endpoint **não aceita JSON**, apenas Protobuf, mas há problema na decodificação.

### Tentativa 4: Ajustes de Timeout e Retry
```yaml
timeout: 30s
retry_on_failure:
  enabled: true
  max_elapsed_time: 300s
```
**Erro**: Mesmos erros persistem após retentativas.

**Análise**: Não é problema de timeout, mas sim de incompatibilidade de protocolo/formato.

## 🎯 Causa Raiz

O **Aspire Dashboard foi projetado para receber dados diretamente dos SDKs do OpenTelemetry em aplicações .NET**, não através de um Collector intermediário. 

### Evidências

1. **Documentação Microsoft**: O Aspire usa endpoints OTLP customizados que esperam formato específico do SDK .NET
2. **Logs do Aspire**: Erros de parsing de Protobuf indicam expectativa de formato diferente
3. **Configuração Padrão**: Aspire é configurado para desenvolvimento rápido, não para ambientes com Collector

### Incompatibilidades Identificadas

| Aspecto | Collector | Aspire Dashboard | Compatível? |
|---------|-----------|------------------|-------------|
| **gRPC Handshake** | HTTP/2 padrão | HTTP/2 customizado | ❌ |
| **Protobuf Schema** | OTLP v1.0.0 | OTLP + extensões MS | ⚠️ |
| **HTTP Encoding** | Protobuf/JSON | Apenas Protobuf específico | ❌ |
| **Batch Processing** | Suportado | Limitado | ⚠️ |

## ✅ Solução Recomendada

### Opção 1: Usar Jaeger + Prometheus + Loki (RECOMENDADO)

**Por quê?**
- ✅ Compatibilidade total com OTel Collector
- ✅ Produção-ready
- ✅ Melhor performance
- ✅ Mais features (alertas, retenção, queries avançadas)
- ✅ Comunidade ativa

**Stack**:
```
Apps → OTel Collector → Jaeger (traces)
                     → Prometheus (metrics)
                     → Loki (logs)
                     → Grafana (visualização)
```

### Opção 2: Remover Collector (Para Desenvolvimento Rápido)

**Por quê?**
- ✅ Funciona imediatamente com Aspire
- ✅ Setup simples
- ⚠️ Menos escalável
- ⚠️ Acoplamento com Aspire

**Stack**:
```
Apps → Aspire Dashboard (tudo)
```

**Configuração**:
```yaml
# Cada serviço .NET
environment:
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://apphost:18888
```

### Opção 3: Dual Stack (Desenvolvimento + Produção)

**Para Desenvolvimento**:
```yaml
- OTEL_EXPORTER_OTLP_ENDPOINT=http://apphost:18888  # Aspire direto
```

**Para Produção**:
```yaml
- OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317  # Via Collector
```

## 📊 Comparação de Soluções

| Aspecto | Aspire (Direto) | Jaeger+Prom+Loki | Dual Stack |
|---------|----------------|------------------|------------|
| **Setup** | ⭐⭐⭐⭐⭐ Fácil | ⭐⭐⭐ Médio | ⭐⭐ Complexo |
| **Performance** | ⭐⭐⭐ Bom | ⭐⭐⭐⭐⭐ Excelente | ⭐⭐⭐⭐ Muito Bom |
| **Features** | ⭐⭐ Básico | ⭐⭐⭐⭐⭐ Completo | ⭐⭐⭐⭐ Muito Bom |
| **Produção** | ❌ Não | ✅ Sim | ✅ Sim |
| **Alertas** | ❌ Não | ✅ Sim | ✅ Sim (prod) |
| **Retenção** | ⭐ Memória | ⭐⭐⭐⭐⭐ Configurável | ⭐⭐⭐⭐ Configurável |
| **Manutenção** | ⭐⭐⭐⭐⭐ Baixa | ⭐⭐⭐ Média | ⭐⭐ Alta |

## 🔧 Arquivos de Configuração Corretos

### Para Jaeger (Recomendado)

**`config/otel-collector.yaml`**:
```yaml
exporters:
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true
  
  prometheus:
    endpoint: 0.0.0.0:8889
  
  otlphttp/loki:
    endpoint: http://loki:3100/otlp

service:
  pipelines:
    traces:
      receivers: [otlp]
      exporters: [otlp/jaeger]
    metrics:
      receivers: [otlp]
      exporters: [prometheus]
    logs:
      receivers: [otlp]
      exporters: [otlphttp/loki]
```

**`docker-compose.yml`**:
```yaml
services:
  # Remover apphost

  jaeger:
    image: jaegertracing/all-in-one:latest
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - "16686:16686"

  # Atualizar todos os serviços .NET
  api:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

## 📝 Lições Aprendidas

### 1. Aspire Dashboard é para Desenvolvimento Direto
- Projetado para conexão direta dos SDKs
- Não é substituto para stack de observabilidade completa
- Útil para prototipagem rápida

### 2. OpenTelemetry Collector Requer Backends Compatíveis
- Jaeger, Prometheus, Loki são testados e suportados
- Aspire Dashboard tem implementação OTLP customizada
- Sempre verificar compatibilidade antes de configurar

### 3. Diferença entre Desenvolvimento e Produção
- Desenvolvimento: Simplicidade > Features
- Produção: Escalabilidade, alertas, retenção são essenciais

### 4. Importância de Logs Detalhados
- Logs do Collector foram cruciais para diagnóstico
- Logs do Aspire revelaram problemas de parsing
- Network tracing ajudou identificar handshake HTTP/2

## 🎓 Conhecimento Técnico Adquirido

### gRPC/HTTP/2
- Compreensão de frames SETTINGS vs GOAWAY
- Diferenças entre gRPC padrão e implementações customizadas
- Debugging de handshakes HTTP/2

### Protobuf
- Schemas OTLP v1.0.0
- Diferenças de serialização entre SDKs
- Wire types e encoding

### OpenTelemetry
- Arquitetura Collector (receivers, processors, exporters)
- Diferenças entre OTLP gRPC e HTTP
- Configurações de retry e timeout

## 🔗 Referências Úteis

- [OpenTelemetry Collector Configuration](https://opentelemetry.io/docs/collector/configuration/)
- [Jaeger OTLP Support](https://www.jaegertracing.io/docs/1.50/deployment/#otlp)
- [Aspire Dashboard Limitations](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OTLP Specification](https://opentelemetry.io/docs/specs/otlp/)

## ✅ Próximos Passos

1. **Decisão**: Escolher entre Opção 1 (Jaeger+Prom+Loki) ou Opção 2 (Aspire direto)
2. **Implementação**: Seguir o plano em `PLANO_OBSERVABILIDADE.md`
3. **Validação**: Testar fluxo completo de dados
4. **Documentação**: Atualizar README com nova configuração

---

**Data**: 2026-01-04  
**Tempo de Investigação**: ~2 horas  
**Status**: Diagnosticado e documentado  
**Próxima Ação**: Implementar stack Jaeger+Prometheus+Loki
