using Microsoft.AspNetCore.Components;

namespace csBlazorLifeCycleEvent.Components.Pages;

public partial class Home
{
    public string Name { get; set; } = "Vulcan";

    public int Age { get; set; } = 25;

    public Address Address { get; set; } = new Address();

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var dict = parameters.ToDictionary();
        var json = System.Text.Json.JsonSerializer.Serialize(dict);
        Console.WriteLine($"H1 - Home SetParametersAsync: {DateTime.Now} , {json}");
        await base.SetParametersAsync(parameters);
    }
    override protected void OnInitialized()
    {
        Console.WriteLine($"H2 - Home OnInitialized: {DateTime.Now}");
    }
    override protected async Task OnInitializedAsync()
    {
        Console.WriteLine($"H3 - Home OnInitializedAsync: {DateTime.Now}");
    }
    override protected void OnParametersSet()
    {
        Console.WriteLine($"H4 - Home OnParametersSet: {DateTime.Now}");
    }
    override protected async Task OnParametersSetAsync()
    {
        Console.WriteLine($"H5 - Home OnParametersSetAsync: {DateTime.Now}");
    }
    override protected void OnAfterRender(bool firstRender)
    {
        Console.WriteLine($"H6 - Home OnAfterRender: {DateTime.Now}, firstRender: {firstRender}");
    }
    override protected async Task OnAfterRenderAsync(bool firstRender)
    {
        Console.WriteLine($"H7 - Home OnAfterRenderAsync: {DateTime.Now}, firstRender: {firstRender}");
    }
    protected override bool ShouldRender()
    {
        Console.WriteLine($"H8 - Home ShouldRender: {DateTime.Now}");
        return base.ShouldRender();
    }
    void OnButtonNothingClick()
    {
        Console.WriteLine();
        Console.WriteLine();
    }
    void OnButtonChange1ParameterClick()
    {
        Console.WriteLine();
        Console.WriteLine();
        Name = "Spock";
    }
    void OnButtonChange2ParameterClick()
    {
        Console.WriteLine();
        Console.WriteLine();
        Name = "Tom";
        Age = 35;
    }
    void OnButtonChangeObjectPropertyParameterClick()
    {
        Console.WriteLine();
        Console.WriteLine();
        Address.Country = "USA";
        Address.City = "New York";
    }
}
