# Spec: Account Services

## ADDED Requirements

### Requirement: Check Account Balance
The system MUST allow querying the current balance of a specific bank account.

#### Scenario: Successfully check balance
- **Given** an existing Account ID.
- **When** the Get Balance endpoint is called.
- **Then** the current available balance is returned.

### Requirement: Get Account Details
The system MUST allow retrieving general account information.

#### Scenario: Retrieve account details
- **Given** an existing Account ID.
- **When** the Get Account endpoint is called.
- **Then** the account number, branch, and status are returned.
