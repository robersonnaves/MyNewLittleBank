-- Create tables using PostgreSQL xmin system column for concurrency control
-- xmin is a built-in system column that stores the transaction ID that inserted the row

CREATE TABLE clients (
    id UUID PRIMARY KEY,
    cpf VARCHAR(11) NOT NULL,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(320) NOT NULL,
    mobile_number VARCHAR(32) NOT NULL,
    CONSTRAINT uk_clients_cpf UNIQUE (cpf)
);

CREATE INDEX ix_clients_cpf ON clients (cpf);

CREATE TABLE bank_accounts (
    id UUID PRIMARY KEY,
    client_id UUID NOT NULL,
    account_number VARCHAR(20) NOT NULL,
    balance NUMERIC(18,2) NOT NULL,
    opened_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT uk_bank_accounts_account_number UNIQUE (account_number),
    CONSTRAINT fk_bank_accounts_clients_client_id FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE RESTRICT
);

CREATE INDEX ix_bank_accounts_account_number ON bank_accounts (account_number);
CREATE INDEX ix_bank_accounts_client_id ON bank_accounts (client_id);

CREATE TABLE transactions (
    id UUID PRIMARY KEY,
    client_id UUID NOT NULL,
    bank_account_id VARCHAR(20) NOT NULL,
    amount NUMERIC(18,2) NOT NULL,
    status VARCHAR(32) NOT NULL,
    occurred_on TIMESTAMP WITH TIME ZONE NOT NULL,
    transaction_type VARCHAR(13) NOT NULL,
    card_number VARCHAR(32),
    origin_pix_key VARCHAR(200),
    destination_pix_key VARCHAR(200),
    CONSTRAINT fk_transactions_clients_client_id FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE RESTRICT,
    CONSTRAINT fk_transactions_bank_accounts_bank_account_id FOREIGN KEY (bank_account_id) REFERENCES bank_accounts(account_number) ON DELETE RESTRICT
);

CREATE INDEX ix_transactions_bank_account_id ON transactions (bank_account_id);
CREATE INDEX ix_transactions_client_id ON transactions (client_id);
CREATE INDEX ix_transactions_transaction_type ON transactions (transaction_type);

CREATE TABLE inbox_messages (
    message_id UUID NOT NULL,
    consumer VARCHAR(128) NOT NULL,
    processed_on_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    PRIMARY KEY (message_id, consumer)
);

CREATE INDEX ix_inbox_messages_consumer ON inbox_messages (consumer);

CREATE TABLE outbox_messages (
    message_id UUID PRIMARY KEY,
    message_type VARCHAR(128) NOT NULL,
    payload JSONB NOT NULL,
    occurred_on_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    sent_on_utc TIMESTAMP WITH TIME ZONE,
    status VARCHAR(32) NOT NULL DEFAULT 'pending',
    attempts INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX ix_outbox_messages_status ON outbox_messages (status);
