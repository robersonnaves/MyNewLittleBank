# Change: Remover todos os warnings da solução

## Why
Builds e testes geram warnings recorrentes (p.ex. CA1848 sobre logging, S1144 sobre setters não usados) que poluem o pipeline e podem mascarar problemas reais. Precisamos zerar os warnings respeitando o doc/code-style-guide.md para manter higiene da base e qualidade dos sinais.

## What Changes
- Levantar e corrigir todos os warnings atuais de compilação/analisadores da solução, alinhando-se às regras de estilo/documentação vigente.
- Ajustar código ou configurações mínimas necessárias para eliminar os avisos sem silenciar regras indevidamente.
- Garantir que builds/tests futuros rodem limpos (0 warnings) sob as mesmas configurações de análise.

## Impact
- Affected specs: code-quality (novo requisito de build sem warnings)
- Affected code: ajustes pontuais em Domain, UseCases e demais projetos que emitirem warnings; configurações de analisadores/estilo se necessário.
