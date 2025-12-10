# Tasks: Add Swagger/OpenAPI documentation

## 1. Swagger setup and configuration
- [x] 1.1 Confirm Swashbuckle dependency and enable XML documentation output in the API project.
- [x] 1.2 Registrar `AddEndpointsApiExplorer` e `AddSwaggerGen` com metadados (título, versão, descrição, contato) alinhados ao serviço.
- [x] 1.3 Expor `/swagger` e `/swagger/v1/swagger.json` em ambientes não produtivos e manter desativado em produção por padrão (com flag de configuração para habilitar se necessário).
- [x] 1.4 Anotar endpoints com respostas de sucesso/erro para refletir no contrato OpenAPI.
- [x] 1.5 Validar manualmente a UI do Swagger e o documento JSON gerado (bloqueado: `dotnet restore/build` falhou no ambiente atual, impedindo a validação local).
