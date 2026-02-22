using Microsoft.AspNetCore.Components;

namespace csBlazorLifeCycleEvent.Components.Pages;

public partial class ChildView
{
    [Parameter]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public int Age { get; set; }
    //[Parameter]
    public Address Address { get; set; } = new();
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var dict = parameters.ToDictionary();
        var json = System.Text.Json.JsonSerializer.Serialize(dict);
        Console.WriteLine($"C1 - Child SetParametersAsync: {DateTime.Now} , {json}");
        if (parameters.TryGetValue<string>("Name", out var newName))
        {
            if (Name != newName)
                Console.WriteLine($"     收到新的 Name: {newName}");
        }
        if (parameters.TryGetValue<int>("Age", out var newAge))
        {
            if (Age != newAge)
                Console.WriteLine($"     收到新的 Age: {newAge}");
        }
        await base.SetParametersAsync(parameters);
    }
    override protected void OnInitialized()
    {
        Console.WriteLine($"C2 - Child OnInitialized: {DateTime.Now}");
    }
    override protected async Task OnInitializedAsync()
    {
        Console.WriteLine($"C3 - Child OnInitializedAsync: {DateTime.Now}");
    }
    override protected void OnParametersSet()
    {
        Console.WriteLine($"C4 - Child OnParametersSet: {DateTime.Now}");
    }
    override protected async Task OnParametersSetAsync()
    {
        Console.WriteLine($"C5 - Child OnParametersSetAsync: {DateTime.Now}");
    }
    override protected void OnAfterRender(bool firstRender)
    {
        Console.WriteLine($"C6 - Child OnAfterRender: {DateTime.Now}, firstRender: {firstRender}");
    }
    override protected async Task OnAfterRenderAsync(bool firstRender)
    {
        Console.WriteLine($"C7 - Child OnAfterRenderAsync: {DateTime.Now}, firstRender: {firstRender}");
    }
    protected override bool ShouldRender()
    {
        Console.WriteLine($"C8 - Child ShouldRender: {DateTime.Now}");
        return base.ShouldRender();
    }
}
