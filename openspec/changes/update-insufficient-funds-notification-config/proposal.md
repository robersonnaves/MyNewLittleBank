# Change: Parametrizar endpoint e habilitação de notificações de saldo insuficiente

## Why
O envio de notificações de saldo insuficiente está fixo e sempre ativo. Precisamos permitir configurar o endpoint via appsettings/variável de ambiente e habilitar/desabilitar o envio conforme ambiente/política, sem quebrar o fluxo principal.

## What Changes
- Tornar o endpoint da API de notificações parametrizável por configuração (appsettings/env) no pipeline de notificações de saldo insuficiente.
- Permitir habilitar/desabilitar o envio de notificações de saldo insuficiente via configuração, mantendo o comportamento atual como padrão seguro.
- Documentar o comportamento esperado quando o envio estiver desabilitado (ex.: não chamar endpoint, registrar telemetria mínima).

## Impact
- Affected specs: account-services (requisito de notificações de saldo insuficiente passa a ser configurável)
- Affected code: UseCases notifications config/options wiring; DI/handlers que disparam notificação.
