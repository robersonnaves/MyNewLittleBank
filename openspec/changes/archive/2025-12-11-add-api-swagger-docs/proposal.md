# Change: Add Swagger/OpenAPI documentation to the API

## Why
A API ainda não possui uma documentação formal acessível para os consumidores. Precisamos expor e padronizar o contrato HTTP para acelerar a descoberta dos endpoints e evitar divergências entre código e expectativa de clientes internos.

## What Changes
- Habilitar geração do documento OpenAPI com metadados claros (título, versão, descrição e contato).
- Expor Swagger UI e o JSON do contrato em ambientes não produtivos, com proteção para produção.
- Documentar respostas de sucesso e erro dos endpoints existentes para manter o contrato alinhado ao comportamento implementado.

## Impact
- Affected specs: api-documentation
- Affected code: src/API/Program.cs, src/API/API.csproj, src/API/Endpoints/*
