# Design: Swagger/OpenAPI documentation

## Context
A API mínima já expõe endpoints para clientes e contas, mas não há contrato documentado e testável para consumidores. Precisamos de documentação interativa para acelerar a integração interna e garantir rastreabilidade das respostas.

## Goals / Non-Goals
- Goals: Disponibilizar o documento OpenAPI e Swagger UI; padronizar metadados do serviço; refletir respostas de sucesso/erro dos endpoints; limitar exposição em produção por padrão.
- Non-Goals: Implementar autenticação na UI do Swagger; versionamento avançado do documento; geração de clientes; alterar lógica de negócio dos endpoints.

## Decisions
- Usar `Swashbuckle.AspNetCore` com `AddEndpointsApiExplorer` para Minimal APIs (já alinhado ao stack .NET 8).
- Configurar metadados (título, versão, descrição, contato) centralizados em `Program.cs` e vinculados ao serviço `api`.
- Ativar UI e JSON do Swagger apenas em `Development`/`Staging` (ou quando um `Swagger:Enabled=true` for definido), para evitar exposição em produção.
- Incluir comentários XML e atributos `Produces`/`ProducesResponseType` nos endpoints para documentar contratos de sucesso e erro.

## Risks / Trade-offs
- Exposição indevida em produção se o flag for habilitado sem proteção → mitigar com default desativado e checagem de ambiente.
- Documentação ficar desatualizada caso endpoints não sejam anotados → mitigar com checklist de respostas obrigatórias e validação manual.

## Migration Plan
1. Garantir referência ao Swashbuckle e habilitar geração de XML docs no `csproj`.
2. Registrar Swagger/OpenAPI no container de serviços com metadados.
3. Configurar `UseSwagger`/`UseSwaggerUI` somente em ambientes permitidos ou quando habilitado via configuração.
4. Anotar endpoints com respostas mapeadas para refletir no contrato.
5. Validar UI e JSON gerado.

## Open Questions
- Devemos permitir UI em produção atrás de autenticação ou apenas via feature flag temporária?
