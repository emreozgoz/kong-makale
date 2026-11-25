var builder = WebApplication.CreateBuilder(args);

// products-api'ye dogrudan (Kong'dan GECMEDEN) gidecek client.
// Servisler arasi trafik ic ag uzerinden, gateway'e ugramadan akar.
builder.Services.AddHttpClient("products", client =>
{
    var baseUrl = builder.Configuration["PRODUCTS_API_URL"] ?? "http://products-api:8080";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

var orders = new List<Order>
{
    new(1001, 1, 2, "completed"),
    new(1002, 3, 1, "pending"),
};
var nextId = 1003;

// --- 1) INTERNAL: siparis listesi ---
app.MapGet("/orders", () => Results.Ok(orders));

// --- 2) INTERNAL: siparis detayi ---
app.MapGet("/orders/{id:int}", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    return order is null ? Results.NotFound(new { message = $"Siparis bulunamadi: {id}" })
                         : Results.Ok(order);
});

// --- 3) INTERNAL: yeni siparis. products-api'yi ic agdan cagirir. ---
app.MapPost("/orders", async (CreateOrder request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("products");

    HttpResponseMessage response;
    try
    {
        response = await client.GetAsync($"/products/{request.ProductId}");
    }
    catch (Exception ex)
    {
        return Results.Problem($"products-api'ye ulasilamadi: {ex.Message}");
    }

    if (!response.IsSuccessStatusCode)
        return Results.BadRequest(new { message = $"Gecersiz urun: {request.ProductId}" });

    var order = new Order(nextId++, request.ProductId, request.Quantity, "pending");
    orders.Add(order);
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "orders-api" }));

app.Run();

record Order(int Id, int ProductId, int Quantity, string Status);
record CreateOrder(int ProductId, int Quantity);
