# api-documentation Specification

## Purpose
TBD - created by archiving change add-api-swagger-docs. Update Purpose after archive.
## Requirements
### Requirement: Expose Swagger UI and OpenAPI specification
The API SHALL expose an OpenAPI document and Swagger UI for its HTTP endpoints while keeping exposure disabled by default in production.

#### Scenario: Available in non-production
- **WHEN** the API runs in Development or Staging (or `Swagger:Enabled=true` is set),
- **THEN** `GET /swagger` and `GET /swagger/v1/swagger.json` return the Swagger UI and OpenAPI document.

#### Scenario: Disabled in production by default
- **WHEN** the API runs in Production without an explicit Swagger enablement flag,
- **THEN** Swagger UI endpoints are not exposed.

### Requirement: Document API metadata and responses
The generated OpenAPI document SHALL include service metadata and reflect success and error responses for the available endpoints.

#### Scenario: OpenAPI document includes service metadata
- **WHEN** requesting the OpenAPI JSON,
- **THEN** it contains the API title, version, description, and contact information configured for the service.

#### Scenario: Operations expose response contracts
- **WHEN** inspecting any documented operation,
- **THEN** the OpenAPI document lists the primary success response and the expected error responses (e.g., validation errors, not found) matching the implemented behavior.

