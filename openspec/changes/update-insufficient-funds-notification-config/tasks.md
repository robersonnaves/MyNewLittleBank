## 1. Implementação
- [x] 1.1 Introduzir configuração para habilitar/desabilitar notificações de saldo insuficiente (env/appsettings) com padrão seguro.
- [x] 1.2 Parametrizar o endpoint de notificações via env/appsettings, mantendo compatibilidade com configuração atual.
- [x] 1.3 Ajustar handler/cliente para respeitar a flag e não chamar o endpoint quando desabilitado, registrando telemetria apropriada.
- [x] 1.4 Atualizar configurações dos serviços (Pix/Money/Card) para expor as novas chaves e validar parsing.
- [x] 1.5 Cobrir com testes de configuração/comportamento (env on/off, endpoint custom) mantendo alinhamento ao doc/code-style-guide.md.

## 2. Validação
- [x] 2.1 Rodar `openspec validate update-insufficient-funds-notification-config --strict`.
- [x] 2.2 Evidenciar testes que confirmem o respeito às flags de configuração.
