using csBlazorRoutingParameter.Components.Pages;

namespace csBlazorRoutingParameter.Services;

public class ProductService
{
    public List<Product> Products { get; set; } = new();
    public ProductService()
    {
        Products = new List<Product>
        {
            new Product { Id = 1, Name = "智慧型手機", Price = 15999, StockQuantity = 120, LaunchDate = new DateTime(2025, 1, 15) },
            new Product { Id = 2, Name = "筆記型電腦", Price = 32000, StockQuantity = 45, LaunchDate = new DateTime(2024, 11, 20) },
            new Product { Id = 3, Name = "無線耳機", Price = 3500, StockQuantity = 200, LaunchDate = new DateTime(2025, 3, 10) },
            new Product { Id = 4, Name = "智慧手錶", Price = 5999, StockQuantity = 75, LaunchDate = new DateTime(2025, 2, 5) },
            new Product { Id = 5, Name = "平板電腦", Price = 12500, StockQuantity = 60, LaunchDate = new DateTime(2024, 12, 1) },
            new Product { Id = 6, Name = "藍牙喇叭", Price = 2800, StockQuantity = 150, LaunchDate = new DateTime(2025, 4, 25) },
            new Product { Id = 7, Name = "遊戲主機", Price = 12000, StockQuantity = 30, LaunchDate = new DateTime(2024, 10, 15) },
            new Product { Id = 8, Name = "無線滑鼠", Price = 1200, StockQuantity = 180, LaunchDate = new DateTime(2025, 5, 10) },
            new Product { Id = 9, Name = "機械鍵盤", Price = 3200, StockQuantity = 90, LaunchDate = new DateTime(2025, 3, 15) },
            new Product { Id = 10, Name = "電競顯示器", Price = 8500, StockQuantity = 40, LaunchDate = new DateTime(2024, 9, 5) }
        };
    }
    public List<Product> GetProducts()
    {
        return Products;
    }
}
