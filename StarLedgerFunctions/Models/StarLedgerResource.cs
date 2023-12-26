namespace StarLedgerFunctions.Models;
public class StarLedgerResource
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Type { get; set; }
    public double PriceBuy { get; set; }
    public double PriceSell { get; set; }
}