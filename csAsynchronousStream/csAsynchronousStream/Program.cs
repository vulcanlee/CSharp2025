using System.Diagnostics;

namespace csAsynchronousStream;

internal class Program
{
    static async Task Main(string[] args)
    {
        var sw = Stopwatch.StartNew();

        Log("=== 非同步串流 Asynchronous Stream 的比較展示 ===");

        await Demo_Stream(sw);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        await Demo_Batch(sw);

        Log("=== 展示結束 ===");
    }

    static void Log(string msg) => Console.WriteLine(msg);

    static void Log(Stopwatch sw, string msg)
        => Console.WriteLine($"{sw.ElapsedMilliseconds,5}ms | {msg}");

    static async Task Demo_Stream(Stopwatch sw)
    {
        Log(sw, "[Stream 串流] 使用 IAsyncEnumerable");

        await foreach (var i in RangeAsync(start: 1, count: 5, delayMs: 300, sw))
        {
            Log(sw, $"[Stream 串流] 接收到 {i} 迭代請求 -> 開始進行處理 ...");
            await Task.Delay(200); // 模擬呼叫端處理耗時
            Log(sw, $"[Stream 串流] 已經處理完成 {i} 請求 -> 請求下一筆");
        }

        Log(sw, "[Stream 串流] 結束");
    }

    static async Task Demo_Batch(Stopwatch sw)
    {
        Log(sw, "[Batch 批次] 開始進行等候 Task<List<int>> (需要等待所有的迭代都完成後，才會繼續往下處理)");

        var list = await RangeTaskAsync(start: 1, count: 5, delayMs: 300, sw);

        Log(sw, $"[Batch 批次] 準備進行處理所有的迭代工作 (count={list.Count}) -> 開始進行處理所有工作...");
        foreach (var i in list)
        {
            Log(sw, $"[Batch 批次] 正在處理 {i} 個工作...");
            await Task.Delay(200); // 模擬呼叫端處理耗時
            Log(sw, $"[Batch 批次] 已經處理完成 {i} 個工作");
        }

        Log(sw, "[Batch 批次] 結束");
    }

    static async IAsyncEnumerable<int> RangeAsync(int start, int count, int delayMs, Stopwatch sw)
    {
        for (int i = start; i < start + count; i++)
        {
            Log(sw, $"[Stream 串流] 準備要產生迭代工作 {i}...");
            await Task.Delay(delayMs);                 // 模擬「取得下一筆資料」耗時
            Log(sw, $"[Stream 串流] 產生出結果給呼叫端 {i}");
            yield return i;                             // 交付給呼叫端，呼叫端可立刻處理
        }
    }

    static async Task<List<int>> RangeTaskAsync(int start, int count, int delayMs, Stopwatch sw)
    {
        var data = new List<int>();

        for (int i = start; i < start + count; i++)
        {
            Log(sw, $"[Batch 批次] 準備要產生迭代工作 {i}...");
            await Task.Delay(delayMs);                 // 模擬「取得下一筆資料」耗時
            data.Add(i);                               // 先收集起來
            Log(sw, $"[Batch 批次] 產生結果集合 {i} (此時將還不會回傳)");
        }

        Log(sw, "[Batch 批次] 全部都處理完成，並且回傳結果");
        return data;
    }
}
