using System.Text;

namespace WebApiDemo;

public sealed class RawHttpLoggingMiddleware
{
    private const int DefaultMaxBodySize = 1024 * 64; // 64 KB
    private static readonly HashSet<string> BinaryContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/octet-stream"
        };

    private readonly RequestDelegate _next;
    private readonly ILogger<RawHttpLoggingMiddleware> _logger;
    private readonly int _maxBodySize;

    public RawHttpLoggingMiddleware(RequestDelegate next,
        ILogger<RawHttpLoggingMiddleware> logger,
        int maxBodySize = DefaultMaxBodySize)
    {
        _next = next;
        _logger = logger;
        _maxBodySize = maxBodySize;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTs = DateTime.UtcNow;

        string requestText = await FormatRequestAsync(context);

        var originalResponseBody = context.Response.Body;
        await using var tempResponseBody = new MemoryStream();
        context.Response.Body = tempResponseBody;

        bool responseCopied = false;

        try
        {
            await _next(context);

            // 從暫存流讀取完整回應內容
            tempResponseBody.Position = 0;
            string bodyText = await ReadBodySafeAsync(context.Response.ContentType, tempResponseBody, tempResponseBody.Length);

            var duration = DateTime.UtcNow - startTs;

            // 複製到真正輸出（第一次寫入會觸發 OnStarting，例如 CORS 標頭）
            tempResponseBody.Position = 0;
            await tempResponseBody.CopyToAsync(originalResponseBody);
            responseCopied = true;

            // 取得（此時已含 OnStarting 加入的）標頭
            var res = context.Response;
            var sb = new StringBuilder()
                .Append(context.Request.Protocol).Append(' ')
                .Append(res.StatusCode).Append(' ')
                .Append(GetReasonPhrase(res.StatusCode)).Append("\r\n");

            foreach (var header in res.Headers)
                foreach (var value in header.Value)
                    sb.Append(header.Key).Append(": ").Append(value).Append("\r\n");

            sb.Append("\r\n").Append(bodyText);
            string responseText = sb.ToString();

            _logger.LogInformation(
                """
===== HTTP 交換開始 =====
{Request}
--------------------------------------------------
{Response}
--------------------------------------------------
耗時: {ElapsedMs} ms
===== HTTP 交換結束 =====
""",
                requestText,
                responseText,
                duration.TotalMilliseconds
            );
        }
        finally
        {
            // 若尚未複製（例如中途異常），仍須把已緩衝的內容送出去
            if (!responseCopied)
            {
                tempResponseBody.Position = 0;
                await tempResponseBody.CopyToAsync(originalResponseBody);
            }

            context.Response.Body = originalResponseBody;
        }
    }

    private async Task<string> FormatRequestAsync(HttpContext context)
    {
        var req = context.Request;
        req.EnableBuffering(); // 允許多次讀取

        string bodyText = await ReadBodySafeAsync(req.ContentType, req.Body, req.ContentLength);

        var sb = new StringBuilder();
        sb.Append(req.Method)
          .Append(' ')
          .Append(req.PathBase.HasValue ? req.PathBase.Value : string.Empty)
          .Append(req.Path.HasValue ? req.Path.Value : string.Empty);

        if (req.QueryString.HasValue)
            sb.Append(req.QueryString.Value);

        sb.Append(' ')
          .Append(req.Protocol)
          .Append("\r\n");

        if (!string.IsNullOrEmpty(req.Host.Value))
            sb.Append("Host: ").Append(req.Host.Value).Append("\r\n");

        foreach (var header in req.Headers)
        {
            if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var value in header.Value)
                sb.Append(header.Key).Append(": ").Append(value).Append("\r\n");
        }

        sb.Append("\r\n");
        if (!string.IsNullOrEmpty(bodyText))
            sb.Append(bodyText);

        req.Body.Position = 0;
        return sb.ToString();
    }

    private async Task<string> FormatResponseAsync(HttpContext context)
    {
        var res = context.Response;
        res.Body.Position = 0;

        string bodyText = await ReadBodySafeAsync(res.ContentType, res.Body, res.ContentLength);

        var sb = new StringBuilder();
        sb.Append(context.Request.Protocol)
          .Append(' ')
          .Append(res.StatusCode)
          .Append(' ')
          .Append(GetReasonPhrase(res.StatusCode))
          .Append("\r\n");

        foreach (var header in res.Headers)
            foreach (var value in header.Value)
                sb.Append(header.Key).Append(": ").Append(value).Append("\r\n");

        sb.Append("\r\n");
        if (!string.IsNullOrEmpty(bodyText))
            sb.Append(bodyText);

        res.Body.Position = 0;
        return sb.ToString();
    }

    private static async Task<string> ReadBodySafeAsync(string? contentType, Stream bodyStream, long? declaredLength)
    {
        if (bodyStream == Stream.Null || !bodyStream.CanRead)
            return string.Empty;

        if (IsBinary(contentType))
            return $"(略過二進位內容: {contentType ?? "未知類型"})";

        var ms = new MemoryStream();
        bodyStream.Position = 0;
        await bodyStream.CopyToAsync(ms);
        bodyStream.Position = 0;

        if (ms.Length == 0)
            return string.Empty;

        if (!LooksLikeText(ms))
            return "(略過非文字內容)";

        ms.Position = 0;
        using var reader = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync();

        const int maxPreview = DefaultMaxBodySize;
        if (text.Length > maxPreview)
            return text.Substring(0, maxPreview) + $"...(截斷，原長度約 {text.Length} chars)";

        return text;
    }

    private static bool IsBinary(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        if (BinaryContentTypes.Contains(contentType))
            return true;

        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            return false;
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return false;
        if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
            return false;
        if (contentType.Contains("form", StringComparison.OrdinalIgnoreCase))
            return false;
        if (contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool LooksLikeText(MemoryStream ms)
    {
        var buf = ms.GetBuffer();
        int len = (int)Math.Min(ms.Length, 512);
        int control = 0;
        for (int i = 0; i < len; i++)
        {
            byte b = buf[i];
            if (b == 0) return false;
            if (b < 0x09) control++;
        }
        return control < 5;
    }

    private static string GetReasonPhrase(int statusCode) =>
        statusCode switch
        {
            200 => "OK",
            201 => "Created",
            202 => "Accepted",
            204 => "No Content",
            301 => "Moved Permanently",
            302 => "Found",
            304 => "Not Modified",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            415 => "Unsupported Media Type",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => string.Empty
        };
}

public static class RawHttpLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRawHttpLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RawHttpLoggingMiddleware>();
}