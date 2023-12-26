using System.Text.Json.Serialization;

namespace StarLedgerFunctions.Models;
public class UexResource
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonPropertyName("trade_price_buy")]
    public double PriceBuy { get; set; }

    [JsonPropertyName("trade_price_sell")]
    public double PriceSell { get; set; }
}

public class UexResponse
{
    [JsonPropertyName("data")]
    public List<UexResource> Data { get; set; }
}