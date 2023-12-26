using System.Text.Json.Serialization;

namespace StarLedgerFunctions.Dtos;
public class LoginResponseDto
{
    [JsonPropertyName("tokenType")]
    public required string TokenType { get; set; }
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; set; }
}