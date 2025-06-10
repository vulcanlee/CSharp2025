namespace csBlazorRoutingParameter.Components.Pages
{
    // 產品類別定義
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime LaunchDate { get; set; }
    }
}
