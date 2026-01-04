using System;
using System.Text;
using System.Text.Json;

namespace SmartApp.Helpers;

public static class JwtHelper
{
    public static string DecodePayload(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            throw new ArgumentException("JWT is null or empty.", nameof(jwt));
        }

        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            throw new ArgumentException("Invalid JWT format.", nameof(jwt));
        }

        string payload = parts[1];

        // Base64Url 轉 Base64
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        byte[] bytes = Convert.FromBase64String(payload);
        string json = Encoding.UTF8.GetString(bytes);

        // 格式化 JSON
        using JsonDocument doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true
        }))
        {
            doc.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}