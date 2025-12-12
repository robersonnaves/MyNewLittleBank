## 1. Implementação
- [x] 1.1 Ajustar geração/seed do Mock.Transactions para suportar chaves Pix de telefone, email, CPF e chave aleatória (GUID) associadas às contas.
- [x] 1.2 Garantir seleção e publicação de Pix apenas com chaves válidas desses tipos, com logs em caso de indisponibilidade.
- [x] 1.3 Investigar e corrigir a persistência do saldo em `bank_accounts` durante o processamento (débito/crédito) e validar que o saldo é salvo no banco.
- [x] 1.4 Adicionar testes/instrumentação que cobrem chaves Pix múltiplas e a atualização de saldo persistido após transações.
