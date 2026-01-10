using System.Text;

namespace csListEncounter;

public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Log Request
        Console.WriteLine("========== HTTP Request ==========");
        Console.WriteLine($"{request.Method} {request.RequestUri}");
        Console.WriteLine();
        
        Console.WriteLine("Headers:");
        foreach (var header in request.Headers)
        {
            Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        if (request.Content != null)
        {
            Console.WriteLine();
            Console.WriteLine("Content Headers:");
            foreach (var header in request.Content.Headers)
            {
                Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            Console.WriteLine();
            Console.WriteLine("Body:");
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine(body);
        }

        Console.WriteLine("==================================");
        Console.WriteLine();

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Log Response
        Console.WriteLine("========== HTTP Response ==========");
        Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
        Console.WriteLine();
        
        Console.WriteLine("Headers:");
        foreach (var header in response.Headers)
        {
            Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        if (response.Content != null)
        {
            Console.WriteLine();
            Console.WriteLine("Content Headers:");
            foreach (var header in response.Content.Headers)
            {
                Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            // 注意：讀取 response body 可能會影響後續處理
            // 這裡我們先讀取、記錄，然後重新包裝
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine();
            Console.WriteLine("Body (first 1000 chars):");
            Console.WriteLine(responseBody.Length > 1000 
                ? responseBody.Substring(0, 1000) + "..." 
                : responseBody);

            // 重新建立 content，因為已經被讀取過了
            response.Content = new StringContent(responseBody, Encoding.UTF8, "application/json");
        }

        Console.WriteLine("===================================");
        Console.WriteLine();

        return response;
    }
}
