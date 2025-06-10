﻿namespace csBlazorQueryString.Components.Pages;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime LaunchDate { get; set; }
}
