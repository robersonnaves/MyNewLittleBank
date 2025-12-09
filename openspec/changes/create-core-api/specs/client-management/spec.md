# Spec: Client Management

## ADDED Requirements

### Requirement: Create Client
The system MUST allow the creation of new clients with basic personal information.

#### Scenario: Successfully create a new client
- **Given** valid client details (Name, Email, TaxId/CPF).
- **When** the Client creation endpoint is called.
- **Then** a new Client is persisted in the database.
- **And** the unique Client ID is returned.

#### Scenario: Fail to create client with existing TaxId
- **Given** a TaxId that already belongs to another client.
- **When** the Client creation endpoint is called.
- **Then** the system returns a conflict error (409) or validation error.

### Requirement: Get Client Details
The system MUST allow retrieving client details by their unique ID.

#### Scenario: Retrieve existing client
- **Given** a valid Client ID.
- **When** the Get Client endpoint is called.
- **Then** the client's Name, Email, and TaxId are returned.

#### Scenario: Client not found
- **Given** a non-existent Client ID.
- **When** the Get Client endpoint is called.
- **Then** the system returns a Not Found error (404).
