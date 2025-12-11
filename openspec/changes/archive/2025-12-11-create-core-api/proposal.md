# Proposal: Create Core Minimal API

## Context
The project requires a Minimal API to expose core banking functionalities. Current code contains Domain entities but lacks the API layer to interact with them. This proposal introduces the initial API endpoints for Client Management and Account Services as requested.

## Requirements
### Client Management
- **POST /clients**: Create a new client.
- **GET /clients/{id}**: Retrieve client details.

### Account Services
- **GET /accounts/{id}/balance**: Get current account balance.
- **POST /accounts/{id}/transactions**: (Implied) Support handling transactions if needed, but specifically requested "serviços da conta corrente" - keeping minimal to Balance for now as explicit "consulta do saldo" was requested.
- **Account Services**: General term "serviços da conta corrente" implies more than just balance, but I will start with Balance and structural endpoints as the foundation.

## Out of Scope
- Authentication/Authorization (for this initial minimal scope).
- Complex transaction processing (focusing on the structure and read operations first, unless "serviços" implies full CRUD).
- UI implementation.
