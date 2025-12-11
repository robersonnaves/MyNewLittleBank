# mock-transactions Specification

## Purpose
TBD - created by archiving change update-mock-transactions-precreate-clients-accounts. Update Purpose after archive.
## Requirements
### Requirement: Transações de mock usam clientes e contas registradas
O produtor `Mock.Transactions` SHALL pré-registrar ou reutilizar clientes e contas na API antes de emitir transações, e SHALL montar transações apenas com IDs dessas entidades registradas.

#### Scenario: Seed cria clientes e contas antes de publicar
- **WHEN** o serviço inicia e não possui clientes/contas alvo conhecidos,
- **THEN** ele chama a API (`src/API/API.csproj`) para criar a quantidade configurada de clientes e contas e só passa a publicar transações após receber sucesso e capturar os identificadores.

#### Scenario: Reutilização de registros existentes
- **WHEN** clientes ou contas alvo já existem na API (ex.: seed prévio),
- **THEN** o Mock.Transactions reutiliza esses registros sem duplicar cadastros, mantendo uma lista de IDs válidos para geração.

#### Scenario: Bloqueio seguro em falha de seed
- **WHEN** o seed não consegue criar ou recuperar clientes/contas (erro de API, autenticação, validação),
- **THEN** o Mock.Transactions não publica transações órfãs, registra o erro e aborta ou retenta o seed conforme configuração.

