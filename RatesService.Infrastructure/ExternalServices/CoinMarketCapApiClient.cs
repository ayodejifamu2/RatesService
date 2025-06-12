using System.Text.Json;
using RatesService.Infrastructure.ExternalServices.Contracts;

namespace RatesService.Infrastructure.ExternalServices;

public class CoinMarketCapApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://pro-api.coinmarketcap.com/v1/";

    public CoinMarketCapApiClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<CoinMarketCapResponse> GetLatestListingsAsync()
    {
        var response = await _httpClient.GetAsync("cryptocurrency/listings/latest?convert=USD");
        response.EnsureSuccessStatusCode(); // Throws if not 2xx

        var jsonString = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CoinMarketCapResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    
}