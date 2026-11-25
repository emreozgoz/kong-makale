var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Basit in-memory veri
var products = new List<Product>
{
    new(1, "Klavye",   1250.00m, 42),
    new(2, "Mouse",     680.50m, 17),
    new(3, "Monitor",  8900.00m,  5),
    new(4, "Kulaklik", 2300.00m,  0),
};

// --- 1) PUBLIC: urun listesi. Kong uzerinden disariya acilan TEK endpoint bu. ---
app.MapGet("/products", () =>
    Results.Ok(products.Select(p => new { p.Id, p.Name, p.Price })));

// --- 2) INTERNAL: tek urun detayi (stok bilgisi dahil) ---
app.MapGet("/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null ? Results.NotFound(new { message = $"Urun bulunamadi: {id}" })
                           : Results.Ok(product);
});

// --- 3) INTERNAL: is metrikleri. Disariya asla acilmamali. ---
app.MapGet("/internal/stats", () => Results.Ok(new
{
    totalProducts = products.Count,
    outOfStock    = products.Count(p => p.Stock == 0),
    inventoryValue = products.Sum(p => p.Price * p.Stock),
    secret = "bu-veri-disariya-cikmamali"
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "products-api" }));

app.Run();

record Product(int Id, string Name, decimal Price, int Stock);
