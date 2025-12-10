# Database Reset Scripts

Scripts para resetar completamente o banco de dados e migrations do MyNewLittleBank.

## Scripts Disponíveis

### PowerShell (Windows)
```powershell
.\reset-database.ps1 [opções]
```

### Bash (Linux/macOS)
```bash
./reset-database.sh [opções]
```

## Opções

- `--force` / `-Force`: Pular confirmação de prompts
- `--backup` / `-Backup`: Criar backup do banco antes de dropar
- `--connection-string <string>` / `-ConnectionString <string>`: Sobrescrever connection string padrão

## O Que os Scripts Fazem

1. ✅ Conectam ao PostgreSQL usando a connection string do `appsettings.json`
2. ✅ Criam backup do banco (se flag `--backup` for usada)
3. ✅ Dropam o banco de dados `mynewlittlebank`
4. ✅ Removem todos os arquivos de migration em `src/Infra.Database/Migrations/`
5. ✅ Criam nova migration inicial `InitialCreate`
6. ✅ Aplicam a migration criando todas as tabelas

## Pré-requisitos

- PostgreSQL rodando (via docker-compose ou local)
- `dotnet ef` tool instalado (os scripts instalam automaticamente se não encontrado)
- `psql` disponível no PATH (para operações de drop database)
- `pg_dump` disponível no PATH (apenas se usar flag `--backup`)

## Exemplos de Uso

### Uso Básico
```powershell
# PowerShell
.\reset-database.ps1

# Bash
./reset-database.sh
```

### Com Backup
```powershell
# PowerShell
.\reset-database.ps1 -Backup

# Bash
./reset-database.sh --backup
```

### Sem Confirmação (CI/CD)
```powershell
# PowerShell
.\reset-database.ps1 -Force

# Bash
./reset-database.sh --force
```

### Connection String Customizada
```powershell
# PowerShell
.\reset-database.ps1 -ConnectionString "Host=192.168.1.10;Port=5432;Database=testdb;Username=admin;Password=secret"

# Bash
./reset-database.sh --connection-string "Host=192.168.1.10;Port=5432;Database=testdb;Username=admin;Password=secret"
```

### Combinando Opções
```powershell
# PowerShell
.\reset-database.ps1 -Force -Backup

# Bash
./reset-database.sh --force --backup
```

## Instalação do psql/pg_dump (Se Necessário)

### Windows
```powershell
# Via Chocolatey
choco install postgresql

# Ou baixar de https://www.postgresql.org/download/windows/
```

### Linux (Debian/Ubuntu)
```bash
sudo apt-get install postgresql-client
```

### macOS
```bash
brew install postgresql
```

## Connection String Padrão

Os scripts usam a connection string de `src/API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mynewlittlebank;Username=postgres;Password=S3gr3d0123"
  }
}
```

## Troubleshooting

### Erro: "psql: command not found"
- Instale o PostgreSQL client conforme instruções acima
- Adicione o PostgreSQL bin ao PATH

### Erro: "Database does not exist"
- Normal se for a primeira execução
- O script continua e cria o banco do zero

### Erro: "Cannot drop the currently open database"
- Certifique-se de não ter conexões ativas ao banco
- Feche todas as aplicações e ferramentas conectadas ao banco

### Erro: "Permission denied"
- No Linux/macOS, execute `chmod +x reset-database.sh`
- Verifique as credenciais do PostgreSQL

## Notas Importantes

⚠️ **ATENÇÃO**: Estes scripts são **DESTRUTIVOS**. Eles apagam completamente o banco de dados e todas as migrations existentes. Use com cuidado em ambientes de produção.

✅ **Recomendação**: Sempre use a flag `--backup` quando executar em ambiente que não seja development local.

✅ **CI/CD**: Use a flag `--force` para automação sem prompts interativos.
