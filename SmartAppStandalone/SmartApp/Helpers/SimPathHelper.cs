using System.Text;
using System.Text.Json;

namespace SmartApp.Helpers;

public class SimPathHelper
{
    public static string ExtractSimPayload(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid URL.", nameof(url));

        // AbsolutePath 例如：/v/r3/sim/<payload>/fhir/metadata
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < segments.Length; i++)
        {
            if (string.Equals(segments[i], "sim", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= segments.Length)
                    throw new InvalidOperationException("Found 'sim' segment but no payload segment after it.");

                var payload = segments[i + 1];

                // 基本防呆：payload 不應該是 fhir/metadata 這類固定字
                if (string.IsNullOrWhiteSpace(payload) ||
                    string.Equals(payload, "fhir", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Payload segment after 'sim' is invalid.");

                return payload;
            }
        }

        throw new InvalidOperationException("No 'sim' segment found in URL path.");
    }

    public static string? GetPatientIdFromSmartHealthItSimSegment(string simSegment)
    {
        // simSegment 例如：eyJrIjoiMSIsImIiOiJiMWYwMzY1ZC1mNDA1LTQ1YzAtOGNiZC1kYTU2NTE4ZTc1MDQifQ
        var json = Base64UrlDecode(simSegment);
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.TryGetProperty("b", out var b)
            ? b.GetString()
            : null;
    }

    public static string Base64UrlDecode(string input)
    {
        // Base64URL 轉換為標準 Base64
        string base64 = input.Replace('-', '+').Replace('_', '/');

        // 補齊 padding
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        // 解碼
        byte[] bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    public static string Base64UrlDecodeToString(string input)
    {
        // base64url: '-' -> '+', '_' -> '/'
        var s = input.Replace('-', '+').Replace('_', '/');

        // padding
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        var bytes = Convert.FromBase64String(s);
        return Encoding.UTF8.GetString(bytes);
    }
}
