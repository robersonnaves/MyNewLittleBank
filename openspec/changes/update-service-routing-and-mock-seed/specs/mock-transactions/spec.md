## ADDED Requirements

### Requirement: Seed cria 10 clientes com 1-3 contas cada
O `Mock.Transactions` SHALL criar exatamente 10 clientes durante o seed, atribuindo para cada um um número aleatório de contas entre 1 e 3 (inclusive) antes de publicar transações.

#### Scenario: Seed gera 10 clientes
- **WHEN** o serviço inicializa com seed habilitado,
- **THEN** ele cria (ou reutiliza) 10 clientes via API e mantém seus identificadores para uso nas transações.

#### Scenario: Cada cliente tem de 1 a 3 contas
- **WHEN** o seed cria contas para cada cliente,
- **THEN** cada cliente recebe aleatoriamente de 1 a 3 contas, garantindo pelo menos uma conta por cliente antes de qualquer publicação.

#### Scenario: Validação dos limites de seed
- **WHEN** a configuração do seed é carregada na inicialização,
- **THEN** valores fora dos limites (clientes ≠ 10, contas fora de 1-3) são rejeitados/validados, impedindo a publicação até correção.
