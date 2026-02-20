using Microsoft.AspNetCore.Components;

namespace csNlog55.Components.Pages;

public partial class Counter
{
    private int currentCount = 0;

    [Inject]
    private ILogger<Counter> Logger { get; set; } = default!;

    private void IncrementCount()
    {
        currentCount++;
        Logger.LogInformation("Current count: {CurrentCount}", currentCount);
    }

}
