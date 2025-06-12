namespace RatesService.Infrastructure.ExternalServices.Contracts;

public class CoinMarketCapResponse
{
    public List<Data> data { get; set; }
    public Status status { get; set; }
}

public class Data
{
    public int id { get; set; }
    public string name { get; set; }
    public string symbol { get; set; }
    public string slug { get; set; }
    public int cmc_rank { get; set; }
    public int num_market_pairs { get; set; }
    public int circulating_supply { get; set; }
    public int total_supply { get; set; }
    public int max_supply { get; set; }
    public bool infinite_supply { get; set; }
    public DateTime last_updated { get; set; }
    public string date_added { get; set; }
    public List<string> tags { get; set; }
    public object platform { get; set; }
    public object self_reported_circulating_supply { get; set; }
    public object self_reported_market_cap { get; set; }
    public Quote quote { get; set; }
}

public class Quote
{
    public USD USD { get; set; }
    public BTC BTC { get; set; }
    public ETH ETH { get; set; }
}

public class USD
{
    public decimal price { get; set; }
    public long volume_24h { get; set; }
    public double volume_change_24h { get; set; }
    public double percent_change_1h { get; set; }
    public double percent_change_24h { get; set; }
    public double percent_change_7d { get; set; }
    public double market_cap { get; set; }
    public int market_cap_dominance { get; set; }
    public double fully_diluted_market_cap { get; set; }
    public string last_updated { get; set; }
}

public class BTC
{
    public int price { get; set; }
    public int volume_24h { get; set; }
    public int volume_change_24h { get; set; }
    public int percent_change_1h { get; set; }
    public int percent_change_24h { get; set; }
    public int percent_change_7d { get; set; }
    public int market_cap { get; set; }
    public int market_cap_dominance { get; set; }
    public double fully_diluted_market_cap { get; set; }
    public string last_updated { get; set; }
}

public class ETH
{
    public int price { get; set; }
    public int volume_24h { get; set; }
    public double volume_change_24h { get; set; }
    public int percent_change_1h { get; set; }
    public int percent_change_24h { get; set; }
    public int percent_change_7d { get; set; }
    public int market_cap { get; set; }
    public int market_cap_dominance { get; set; }
    public double fully_diluted_market_cap { get; set; }
    public string last_updated { get; set; }
}

public class Status
{
    public string timestamp { get; set; }
    public int error_code { get; set; }
    public string error_message { get; set; }
    public int elapsed { get; set; }
    public int credit_count { get; set; }
}

