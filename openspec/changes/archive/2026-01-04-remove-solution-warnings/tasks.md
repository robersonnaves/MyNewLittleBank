## 1. Implementação
- [x] 1.1 Inventariar todos os warnings atuais via `dotnet build/test` e registrar alvos (CA/Sxxxx/etc.) respeitando doc/code-style-guide.md.
- [x] 1.2 Corrigir warnings de código (ex.: CA1848, S1144, CA1056) com ajustes mínimos alinhados ao guia; evitar suprimir regras sem justificativa.
- [x] 1.3 Ajustar configurações de analisadores/estilo apenas quando necessário para aderir ao guia sem mascarar problemas.
- [x] 1.4 Confirmar que builds e testes rodam com 0 warnings usando as mesmas configurações.

## 2. Validação
- [x] 2.1 Rodar `openspec validate remove-solution-warnings --strict`.
- [x] 2.2 Registrar evidências de `dotnet build/test` sem warnings.
