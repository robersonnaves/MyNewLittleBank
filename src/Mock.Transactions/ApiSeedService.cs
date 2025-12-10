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

        if (settings.Seed.Clients <= 0 || settings.Seed.AccountsPerClient <= 0)
        {
            _logger.LogError("Seed configuration invalid: Clients={Clients}, AccountsPerClient={AccountsPerClient}.", settings.Seed.Clients, settings.Seed.AccountsPerClient);
            return null;
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

            for (var accountIndex = 0; accountIndex < settings.Seed.AccountsPerClient; accountIndex++)
            {
                var accountNumber = GenerateAccountNumber(clientIndex, accountIndex);
                var account = await GetOrCreateAccountAsync(client.Id, accountNumber, settings.Seed.InitialBalance, settings.Seed.ReuseExisting, cancellationToken).ConfigureAwait(false);
                if (account is null)
                {
                    _logger.LogError("Stopping seed after account {AccountNumber} failed.", accountNumber);
                    return null;
                }

                seededAccounts.Add(new SeededAccount(client.Id, account.AccountNumber));
            }
        }

        _logger.LogInformation(
            "Seed completed via API at {ApiBase}. Clients={Clients}, Accounts={Accounts}.",
            _httpClient.BaseAddress,
            settings.Seed.Clients,
            seededAccounts.Count);

        return seededAccounts;
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
        return number.ToString();
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

    private async Task<ClientResponse?> GetOrCreateClientAsync(SeedClient seed, bool reuseExisting, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("clients", new ApiCreateClientRequest(seed.Cpf, seed.Name, seed.Email, seed.MobileNumber), SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ClientResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.Conflict && reuseExisting)
            {
                return await GetClientByCpfAsync(seed.Cpf, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogError("Failed to create client {Cpf}. StatusCode={StatusCode}", seed.Cpf, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception creating client {Cpf}.", seed.Cpf);
            return null;
        }
    }

    private async Task<ClientResponse?> GetClientByCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"clients/cpf/{cpf}", cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Client with CPF {Cpf} not found when attempting reuse.", cpf);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve client by CPF {Cpf}. StatusCode={StatusCode}", cpf, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ClientResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception fetching client by CPF {Cpf}.", cpf);
            return null;
        }
    }

    private async Task<AccountResponse?> GetOrCreateAccountAsync(Guid clientId, string accountNumber, decimal initialBalance, bool reuseExisting, CancellationToken cancellationToken)
    {
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
                return await response.Content.ReadFromJsonAsync<AccountResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.Conflict && reuseExisting)
            {
                var existing = await GetAccountAsync(accountNumber, cancellationToken).ConfigureAwait(false);
                if (existing is not null && existing.ClientId == clientId)
                {
                    return existing;
                }

                _logger.LogError(
                    "Account {AccountNumber} already exists but is linked to another client ({ExistingClientId}).",
                    accountNumber,
                    existing?.ClientId);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Client {ClientId} not found while creating account {AccountNumber}.", clientId, accountNumber);
                return null;
            }

            _logger.LogError("Failed to create account {AccountNumber}. StatusCode={StatusCode}", accountNumber, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception creating account {AccountNumber}.", accountNumber);
            return null;
        }
    }

    private async Task<AccountResponse?> GetAccountAsync(string accountNumber, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"accounts/{accountNumber}", cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve account {AccountNumber}. StatusCode={StatusCode}", accountNumber, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AccountResponse>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception fetching account {AccountNumber}.", accountNumber);
            return null;
        }
    }

    private sealed record SeedClient(string Cpf, string Name, string Email, string MobileNumber);
    private sealed record ApiCreateClientRequest(string Cpf, string Name, string Email, string MobileNumber);
    private sealed record ApiCreateAccountRequest(Guid ClientId, string AccountNumber, decimal InitialBalance);
    private sealed record ClientResponse(Guid Id, string Name, string Email, string Cpf, string MobileNumber);
    private sealed record AccountResponse(Guid Id, Guid ClientId, string AccountNumber, decimal Balance, DateTime OpenedAt);
}
