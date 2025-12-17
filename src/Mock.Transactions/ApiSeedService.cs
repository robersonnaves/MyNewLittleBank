using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mock.Transactions;

public sealed class ApiSeedService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<MockTransactionsSettings> _settings;
    private readonly ILogger<ApiSeedService> _logger;

    public ApiSeedService(HttpClient httpClient, IOptions<MockTransactionsSettings> settings, ILogger<ApiSeedService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SeededAccount>?> TrySeedAsync(CancellationToken cancellationToken)
    {
        var settings = _settings.Value;
        EnsureBaseAddress(settings);

        if (!settings.Seed.Enabled)
        {
            _logger.LogInformation("Seed disabled via configuration; expecting existing clients/accounts.");
            return Array.Empty<SeededAccount>();
        }

        if (!ValidateSeed(settings))
        {
            _logger.LogError(
                "Seed configuration invalid: Clients={Clients}, MinAccountsPerClient={MinAccountsPerClient}, MaxAccountsPerClient={MaxAccountsPerClient}.",
                settings.Seed.Clients,
                settings.Seed.MinAccountsPerClient,
                settings.Seed.MaxAccountsPerClient);
            return null;
        }

        // Check if database is already seeded before attempting to create data
        if (await IsAlreadySeededAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Database already seeded. Loading existing data...");
            return await LoadExistingSeededDataAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        var seededAccounts = new List<SeededAccount>();

        for (var clientIndex = 0; clientIndex < settings.Seed.Clients; clientIndex++)
        {
            var clientSeed = BuildClientSeed(clientIndex);
            var client = await GetOrCreateClientAsync(clientSeed, settings.Seed.ReuseExisting, cancellationToken).ConfigureAwait(false);
            if (client is null)
            {
                _logger.LogError("Stopping seed after client index {Index} failed.", clientIndex);
                return null;
            }

            var accountsForClient = GetAccountsPerClient(settings.Seed);
            for (var accountIndex = 0; accountIndex < accountsForClient; accountIndex++)
            {
                var accountNumber = GenerateAccountNumber(clientIndex, accountIndex);
                var account = await GetOrCreateAccountAsync(client.Id, accountNumber, settings.Seed.InitialBalance, settings.Seed.ReuseExisting, cancellationToken).ConfigureAwait(false);
                if (account is null)
                {
                    _logger.LogError("Stopping seed after account {AccountNumber} failed.", accountNumber);
                    return null;
                }

                var pixKeys = CreatePixKeysForAccount(client.Id, account.AccountNumber);
                seededAccounts.Add(new SeededAccount(client.Id, account.AccountNumber, pixKeys));
            }
        }

        _logger.LogInformation(
            "Seed completed via API at {ApiBase}. Clients={Clients}, Accounts={Accounts}.",
            _httpClient.BaseAddress,
            settings.Seed.Clients,
            seededAccounts.Count);

        return seededAccounts;
    }

    private static bool ValidateSeed(MockTransactionsSettings settings)
    {
        return settings.Seed.Clients == 10
               && settings.Seed.MinAccountsPerClient >= 1
               && settings.Seed.MaxAccountsPerClient >= settings.Seed.MinAccountsPerClient
               && settings.Seed.MaxAccountsPerClient <= 3;
    }

    private static int GetAccountsPerClient(SeedSettings seedSettings)
    {
        if (seedSettings.MinAccountsPerClient == seedSettings.MaxAccountsPerClient)
        {
            return seedSettings.MinAccountsPerClient;
        }

        return Random.Shared.Next(seedSettings.MinAccountsPerClient, seedSettings.MaxAccountsPerClient + 1);
    }

    private void EnsureBaseAddress(MockTransactionsSettings settings)
    {
        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(settings.ApiBaseUrl, UriKind.Absolute);
        }
    }

    private static SeedClient BuildClientSeed(int index)
    {
        var cpf = GenerateCpf(index + 1);
        var ordinal = index + 1;

        return new SeedClient(
            cpf,
            $"Mock Client {ordinal}",
            $"mock.client{ordinal}@example.com",
            $"1198{(1000000 + index * 137):D7}");
    }

    private static string GenerateAccountNumber(int clientIndex, int accountIndex)
    {
        var number = 10000000 + (clientIndex * 100) + accountIndex;
        return number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GenerateCpf(int seed)
    {
        var random = new Random(seed + 3571);
        var numbers = new int[11];

        for (var i = 0; i < 9; i++)
        {
            numbers[i] = random.Next(0, 10);
        }

        if (numbers.All(n => n == numbers[0]))
        {
            numbers[0] = (numbers[0] + 1) % 10;
        }

        numbers[9] = CalculateCheckDigit(numbers, 9);
        numbers[10] = CalculateCheckDigit(numbers, 10);

        return string.Concat(numbers);
    }

    private static int CalculateCheckDigit(IReadOnlyList<int> numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * ((length + 1) - i);
        }

        var digit = (sum * 10) % 11;
        return digit == 10 ? 0 : digit;
    }

    private async Task<bool> IsAlreadySeededAsync(CancellationToken cancellationToken)
    {
        var firstClientSeed = BuildClientSeed(0);
        var existingClient = await GetClientByCpfAsync(firstClientSeed.Cpf, cancellationToken).ConfigureAwait(false);
        return existingClient is not null;
    }

    private async Task<IReadOnlyList<SeededAccount>?> LoadExistingSeededDataAsync(MockTransactionsSettings settings, CancellationToken cancellationToken)
    {
        var seededAccounts = new List<SeededAccount>();

        for (var clientIndex = 0; clientIndex < settings.Seed.Clients; clientIndex++)
        {
            var clientSeed = BuildClientSeed(clientIndex);
            var client = await GetClientByCpfAsync(clientSeed.Cpf, cancellationToken).ConfigureAwait(false);
            
            if (client is null)
            {
                _logger.LogWarning("Expected client with CPF={Cpf} (index {Index}) not found when loading existing seed data.", clientSeed.Cpf, clientIndex);
                continue;
            }

            // Determine how many accounts this client should have
            // Since we can't know the exact number that was created (it's random), we'll try to find accounts
            // by checking the expected account numbers based on the range
            var maxAccountsToCheck = settings.Seed.MaxAccountsPerClient;
            var accountsFound = 0;

            for (var accountIndex = 0; accountIndex < maxAccountsToCheck; accountIndex++)
            {
                var accountNumber = GenerateAccountNumber(clientIndex, accountIndex);
                var account = await GetAccountAsync(accountNumber, cancellationToken).ConfigureAwait(false);
                
                if (account is not null && account.ClientId == client.Id)
                {
                    var pixKeys = CreatePixKeysForAccount(client.Id, account.AccountNumber);
                    seededAccounts.Add(new SeededAccount(client.Id, account.AccountNumber, pixKeys));
                    accountsFound++;
                }
            }

            if (accountsFound == 0)
            {
                _logger.LogWarning("No accounts found for client with CPF={Cpf} (index {Index}) when loading existing seed data.", clientSeed.Cpf, clientIndex);
            }
        }

        _logger.LogInformation(
            "Loaded existing seed data from API at {ApiBase}. Clients={Clients}, Accounts={Accounts}.",
            _httpClient.BaseAddress,
            seededAccounts.Select(a => a.ClientId).Distinct().Count(),
            seededAccounts.Count);

        return seededAccounts;
    }

    private async Task<ClientResponse?> GetOrCreateClientAsync(SeedClient seed, bool reuseExisting, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to create client with CPF={Cpf}, Name={Name}, Email={Email}, ReuseExisting={ReuseExisting}",
            seed.Cpf, seed.Name, seed.Email, reuseExisting);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("clients", new ApiCreateClientRequest(seed.Cpf, seed.Name, seed.Email, seed.MobileNumber), SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var client = await response.Content.ReadFromJsonAsync<ClientResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully created client with CPF={Cpf}, Id={ClientId}", seed.Cpf, client?.Id);
                return client;
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "HTTP 409 Conflict received when creating client with CPF={Cpf}. ReuseExisting={ReuseExisting}. Client already exists in the system.",
                    seed.Cpf, reuseExisting);

                if (reuseExisting)
                {
                    _logger.LogInformation("ReuseExisting is enabled. Attempting to retrieve existing client with CPF={Cpf}", seed.Cpf);
                    var existingClient = await GetClientByCpfAsync(seed.Cpf, cancellationToken).ConfigureAwait(false);
                    if (existingClient is not null)
                    {
                        _logger.LogInformation("Successfully retrieved existing client with CPF={Cpf}, Id={ClientId}", seed.Cpf, existingClient.Id);
                    }
                    return existingClient;
                }
                else
                {
                    var errorBody = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
                    _logger.LogError(
                        "HTTP 409 Conflict when creating client with CPF={Cpf}, but ReuseExisting={ReuseExisting} is disabled. Error response: {ErrorBody}",
                        seed.Cpf, reuseExisting, errorBody);
                    return null;
                }
            }

            var errorBodyForOtherStatus = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Failed to create client with CPF={Cpf}. StatusCode={StatusCode}, ErrorResponse={ErrorBody}",
                seed.Cpf, response.StatusCode, errorBodyForOtherStatus);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while creating client with CPF={Cpf}.", seed.Cpf);
            return null;
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Client creation cancelled for CPF={Cpf}.", seed.Cpf);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize client response for CPF={Cpf}.", seed.Cpf);
            return null;
        }
#pragma warning disable CA1031 // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
        catch (Exception ex)
        {
            // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
            _logger.LogError(ex, "Unexpected error creating client with CPF={Cpf}.", seed.Cpf);
            return null;
        }
#pragma warning restore CA1031
    }

    private async Task<ClientResponse?> GetClientByCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve client by CPF={Cpf}", cpf);
        try
        {
            var response = await _httpClient.GetAsync(new Uri($"clients/cpf/{cpf}", UriKind.Relative), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Client with CPF={Cpf} not found when attempting reuse.", cpf);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    "Failed to retrieve client by CPF={Cpf}. StatusCode={StatusCode}, ErrorResponse={ErrorBody}",
                    cpf, response.StatusCode, errorBody);
                return null;
            }

            var client = await response.Content.ReadFromJsonAsync<ClientResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully retrieved client with CPF={Cpf}, Id={ClientId}", cpf, client?.Id);
            return client;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while retrieving client by CPF={Cpf}.", cpf);
            return null;
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Client retrieval cancelled for CPF={Cpf}.", cpf);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize client response for CPF={Cpf}.", cpf);
            return null;
        }
#pragma warning disable CA1031 // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
        catch (Exception ex)
        {
            // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
            _logger.LogError(ex, "Unexpected error fetching client by CPF={Cpf}.", cpf);
            return null;
        }
#pragma warning restore CA1031
    }

    private async Task<AccountResponse?> GetOrCreateAccountAsync(Guid clientId, string accountNumber, decimal initialBalance, bool reuseExisting, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to create account with Number={AccountNumber}, ClientId={ClientId}, InitialBalance={InitialBalance}, ReuseExisting={ReuseExisting}",
            accountNumber, clientId, initialBalance, reuseExisting);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                    "accounts",
                    new ApiCreateAccountRequest(clientId, accountNumber, initialBalance),
                    SerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var account = await response.Content.ReadFromJsonAsync<AccountResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully created account with Number={AccountNumber}, Id={AccountId}", accountNumber, account?.Id);
                return account;
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "HTTP 409 Conflict received when creating account with Number={AccountNumber} for ClientId={ClientId}. ReuseExisting={ReuseExisting}. Account already exists in the system.",
                    accountNumber, clientId, reuseExisting);

                if (reuseExisting)
                {
                    _logger.LogInformation("ReuseExisting is enabled. Attempting to retrieve existing account with Number={AccountNumber}", accountNumber);
                    var existing = await GetAccountAsync(accountNumber, cancellationToken).ConfigureAwait(false);
                    if (existing is not null && existing.ClientId == clientId)
                    {
                        _logger.LogInformation(
                            "Successfully retrieved existing account with Number={AccountNumber}, Id={AccountId}, ClientId={ClientId}",
                            accountNumber, existing.Id, existing.ClientId);
                        return existing;
                    }

                    _logger.LogError(
                        "Account {AccountNumber} already exists but is linked to another client. Expected ClientId={ExpectedClientId}, Actual ClientId={ActualClientId}",
                        accountNumber, clientId, existing?.ClientId);
                    return null;
                }
                else
                {
                    var errorBody = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
                    _logger.LogError(
                        "HTTP 409 Conflict when creating account with Number={AccountNumber}, but ReuseExisting={ReuseExisting} is disabled. Error response: {ErrorBody}",
                        accountNumber, reuseExisting, errorBody);
                    return null;
                }
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var errorBody = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    "Client {ClientId} not found while creating account {AccountNumber}. StatusCode={StatusCode}, ErrorResponse={ErrorBody}",
                    clientId, accountNumber, response.StatusCode, errorBody);
                return null;
            }

            var errorBodyForOtherStatus = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Failed to create account {AccountNumber} for ClientId={ClientId}. StatusCode={StatusCode}, ErrorResponse={ErrorBody}",
                accountNumber, clientId, response.StatusCode, errorBodyForOtherStatus);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while creating account {AccountNumber} for ClientId={ClientId}.", accountNumber, clientId);
            return null;
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Account creation cancelled for AccountNumber={AccountNumber}, ClientId={ClientId}.", accountNumber, clientId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize account response for AccountNumber={AccountNumber}.", accountNumber);
            return null;
        }
#pragma warning disable CA1031 // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
        catch (Exception ex)
        {
            // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
            _logger.LogError(ex, "Unexpected error creating account {AccountNumber} for ClientId={ClientId}.", accountNumber, clientId);
            return null;
        }
#pragma warning restore CA1031
    }

    private async Task<AccountResponse?> GetAccountAsync(string accountNumber, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve account with Number={AccountNumber}", accountNumber);
        try
        {
            var response = await _httpClient.GetAsync(new Uri($"accounts/{accountNumber}", UriKind.Relative), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Account with Number={AccountNumber} not found", accountNumber);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await ReadErrorResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    "Failed to retrieve account {AccountNumber}. StatusCode={StatusCode}, ErrorResponse={ErrorBody}",
                    accountNumber, response.StatusCode, errorBody);
                return null;
            }

            var account = await response.Content.ReadFromJsonAsync<AccountResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully retrieved account with Number={AccountNumber}, Id={AccountId}", accountNumber, account?.Id);
            return account;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while retrieving account {AccountNumber}.", accountNumber);
            return null;
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Account retrieval cancelled for AccountNumber={AccountNumber}.", accountNumber);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize account response for AccountNumber={AccountNumber}.", accountNumber);
            return null;
        }
#pragma warning disable CA1031 // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
        catch (Exception ex)
        {
            // Catching all exceptions intentionally to ensure service resilience - any unexpected error should be logged and handled gracefully
            _logger.LogError(ex, "Unexpected error fetching account {AccountNumber}.", accountNumber);
            return null;
        }
#pragma warning restore CA1031
    }

    private async Task<string> ReadErrorResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (response.Content is null)
            {
                return "[No response body]";
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(content) ? "[Empty response body]" : content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP request failed while reading error response body");
            return $"[Error reading response body: {ex.Message}]";
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Reading error response body cancelled");
            return "[Error reading response body: Operation cancelled]";
        }
#pragma warning disable CA1031 // Catching all exceptions intentionally - this is a fallback method that must not throw
        catch (Exception ex)
        {
            // Catching all exceptions intentionally - this is a fallback method that must not throw
            _logger.LogWarning(ex, "Failed to read error response body");
            return $"[Error reading response body: {ex.Message}]";
        }
#pragma warning restore CA1031
    }

    private sealed record SeedClient(string Cpf, string Name, string Email, string MobileNumber);
#pragma warning disable CA1812 // Record types are instantiated via JSON deserialization
    private sealed record ApiCreateClientRequest(string Cpf, string Name, string Email, string MobileNumber);
    private sealed record ApiCreateAccountRequest(Guid ClientId, string AccountNumber, decimal InitialBalance);
    private sealed record ClientResponse(Guid Id, string Name, string Email, string Cpf, string MobileNumber);
    private sealed record AccountResponse(Guid Id, Guid ClientId, string AccountNumber, decimal Balance, DateTime OpenedAt);
#pragma warning restore CA1812

    private static IReadOnlyList<string> CreatePixKeysForAccount(Guid clientId, string accountNumber)
    {
        // Deterministic-ish keys to simulate pre-registered Pix identifiers per account
        var emailKey = $"pix-{accountNumber}@mock.example.com";
        var phoneKey = $"+551198{accountNumber}";

        // Derive a CPF-like key from account number seed
        var cpfSeed = int.TryParse(accountNumber[..Math.Min(accountNumber.Length, 6)], out var parsed)
            ? parsed
            : clientId.GetHashCode();
        var cpfKey = GenerateCpf(Math.Abs(cpfSeed % 999999));

        var guidKey = Guid.NewGuid().ToString("D");

        return new[] { emailKey, phoneKey, cpfKey, guidKey };
    }
}
