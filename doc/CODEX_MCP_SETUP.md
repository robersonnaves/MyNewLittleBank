# Codex MCP Configuration

Este documento descreve a configuração dos servidores MCP (Model Context Protocol) para o OpenAI Codex CLI.

## Configuração

A configuração dos servidores MCP está em `~/.codex/config.toml` (não no projeto). Este arquivo é compartilhado entre o Codex CLI e a extensão VS Code.

## Servidores MCP Configurados

### 1. task-master-ai
Servidor de gerenciamento de tarefas com IA que suporta múltiplos provedores de LLM.

**Comando:**
```bash
npx -y task-master-ai
```

**Variáveis de Ambiente:**
- `ANTHROPIC_API_KEY` - Claude
- `OPENAI_API_KEY` - GPT models
- `PERPLEXITY_API_KEY` - Perplexity
- `GOOGLE_API_KEY` - Gemini
- `MISTRAL_API_KEY` - Mistral
- `XAI_API_KEY` - Grok
- `GROQ_API_KEY` - Groq
- `OPENROUTER_API_KEY` - OpenRouter
- `AZURE_OPENAI_API_KEY` - Azure OpenAI
- `OLLAMA_API_KEY` - Ollama
- `GITHUB_API_KEY` - GitHub

### 2. context7
Servidor de documentação e contexto para assistência no desenvolvimento.

**Comando:**
```bash
npx -y @upstash/context7-mcp
```

## Verificar Configuração

Para listar os servidores MCP configurados:
```bash
codex mcp list
```

Para ver detalhes de um servidor específico:
```bash
codex mcp get task-master-ai
codex mcp get context7
```

## Adicionar/Remover Servidores

Adicionar novo servidor:
```bash
codex mcp add <server-name> --env VAR1=VALUE1 -- <command> [args...]
```

Remover servidor:
```bash
codex mcp remove <server-name>
```

## Variáveis de Ambiente

As variáveis de ambiente devem estar definidas no arquivo `.env` na raiz do projeto e serão carregadas automaticamente pelo Codex quando ele iniciar os servidores MCP.

**Importante:** As chaves de API no `config.toml` usam a sintaxe `$VAR_NAME` para referência a variáveis de ambiente.

## Formato TOML

A configuração usa formato TOML com as seguintes características:

```toml
[mcp_servers.server-name]
command = "comando"
args = ["arg1", "arg2"]
env = { VAR1 = "$VAR1", VAR2 = "$VAR2" }  # Inline table - DEVE estar em uma linha
```

**Atenção:** Inline tables no TOML não podem ter quebras de linha. Todas as variáveis devem estar na mesma linha.

## Referências

- [Documentação oficial do Codex MCP](https://developers.openai.com/codex/mcp/)
- [Config.toml reference](https://github.com/openai/codex/blob/main/docs/config.md)
- [CLI reference](https://developers.openai.com/codex/cli/reference/)
