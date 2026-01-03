using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

namespace SmartApp.Helpers;

public class JwtDecodeHelper
{
    public static (string HeaderJson, string PayloadJson, (string Type, string Value)[] Claims) DecodeWithoutValidation(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("JWT is null or empty.", nameof(jwt));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        var claims = token.Claims
            .Select(c => (c.Type, c.Value))
            .ToArray();

        // Header/Payload 轉成 JSON 字串（方便你 debug 或記錄）
        var headerJson = System.Text.Json.JsonSerializer.Serialize(token.Header);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(token.Payload);

        return (headerJson, payloadJson, claims);
    }
}
