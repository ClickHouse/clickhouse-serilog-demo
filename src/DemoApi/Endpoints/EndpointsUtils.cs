using DemoApi.Models;
using DemoApi.Services;
using Serilog;
namespace DemoApi.Endpoints;

public static class EndpointsUtils
{
   public static void MapEndpoints(this WebApplication webApplication)
    {
    // List products
        webApplication.MapGet("/products", (ProductService products, ILogger<Program> logger) =>
        {
            var all = products.GetAll();
            logger.LogInformation("Product catalog listed — {Count} products", all.Count);
            return Results.Ok(all);
        });

    // Get product by ID
        webApplication.MapGet("/products/{id:int}", (int id, ProductService products, ILogger<Program> logger) =>
        {
            var product = products.GetById(id);
            if (product is null)
            {
                logger.LogWarning("Product {ProductId} not found", id);
                return Results.NotFound(new { error = $"Product {id} not found" });
            }

            logger.LogInformation("Product {ProductId} ({ProductName}) retrieved", product.Id, product.Name);
            return Results.Ok(product);
        });

    // Create product
        webApplication.MapPost("/products", (CreateProductRequest request, ProductService products, ILogger<Program> logger) =>
        {
            var product = products.Create(request);
            logger.LogInformation(
                "Product {ProductId} ({ProductName}) created in category {Category} at {Price:C}",
                product.Id, product.Name, product.Category, product.Price);
            return Results.Created($"/products/{product.Id}", product);
        });

    // Place order
        webApplication.MapPost("/orders", (OrderRequest request, OrderService orders, IDiagnosticContext diagnosticContext, ILogger<Program> logger) =>
        {
            diagnosticContext.Set("UserId", request.UserId);
            diagnosticContext.Set("ProductId", request.ProductId);

            try
            {
                var result = orders.PlaceOrder(request);
                diagnosticContext.Set("OrderId", result.OrderId);
                diagnosticContext.Set("OrderStatus", result.Status);

                return result.Status switch
                {
                    "Confirmed" => Results.Ok(result),
                    _ => Results.UnprocessableEntity(result),
                };
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Order failed — product {ProductId} not found", request.ProductId);
                return Results.NotFound(new { error = ex.Message });
            }
        });

    // Get order (simulated — always returns not found to generate warnings)
        webApplication.MapGet("/orders/{id}", (string id, ILogger<Program> logger) =>
        {
            logger.LogWarning("Order {OrderId} not found (in-memory only, no persistence)", id);
            return Results.NotFound(new { error = $"Order {id} not found" });
        });

    // Chaos endpoint — deliberate exceptions
        webApplication.MapPost("/chaos", (ILogger<Program> logger) =>
        {
            var exceptions = new Exception[]
            {
                new InvalidOperationException("Chaos: invalid state transition"),
                new TimeoutException("Chaos: downstream service timed out"),
                new ArgumentException("Chaos: unexpected argument value"),
            };

            var ex = exceptions[Random.Shared.Next(exceptions.Length)];
            logger.LogError(ex, "Chaos endpoint triggered — {ExceptionType}", ex.GetType().Name);
            return Results.StatusCode(500);
        });

    // Slow endpoint — simulates variable-latency requests for slow query detection
        webApplication.MapGet("/slow", async (ILogger<Program> logger) =>
        {
            var delay = Random.Shared.Next(50, 3000);
            logger.LogInformation("Slow endpoint called, simulating {DelayMs}ms delay", delay);
            await Task.Delay(delay);
            return Results.Ok(new { delayMs = delay });
        });

    // Traffic generator — exercises all endpoints to produce diverse log events
        webApplication.MapPost("/generate-traffic", async (ILogger<Program> logger) =>
        {
            var baseUrl = "http://localhost:8080";
            using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

            var results = new List<string>();

            // Health check
            await http.GetAsync("/health");
            results.Add("GET /health");

            // List products
            await http.GetAsync("/products");
            results.Add("GET /products");

            // Get existing product
            await http.GetAsync("/products/1");
            results.Add("GET /products/1");

            // Get missing product (warning)
            await http.GetAsync("/products/999");
            results.Add("GET /products/999 (not found)");

            // Create a product
            await http.PostAsJsonAsync("/products", new { name = "Standing Desk", category = "Furniture", price = 599.99, stock = 10 });
            results.Add("POST /products (created)");

            // Successful order with explicit correlation ID
            var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/orders")
            {
                Content = JsonContent.Create(new { productId = 1, quantity = 2, userId = "user-42" }),
            };
            orderMsg.Headers.Add("X-Correlation-ID", "demo-corr-001");
            await http.SendAsync(orderMsg);
            results.Add("POST /orders (product 1, with correlation ID)");

            // Low-stock order (warning)
            await http.PostAsJsonAsync("/orders", new { productId = 3, quantity = 1, userId = "user-77" });
            results.Add("POST /orders (product 3, low stock)");

            // Out-of-stock order (error)
            await http.PostAsJsonAsync("/orders", new { productId = 4, quantity = 1, userId = "user-13" });
            results.Add("POST /orders (product 4, out of stock)");

            // Order for missing product (not found)
            await http.PostAsJsonAsync("/orders", new { productId = 999, quantity = 1, userId = "user-99" });
            results.Add("POST /orders (product 999, not found)");

            // Get order (always not found)
            await http.GetAsync("/orders/abc123");
            results.Add("GET /orders/abc123 (not found)");

            // Chaos endpoints
            for (var i = 0; i < 3; i++)
            {
                await http.PostAsync("/chaos", null);
            }
            results.Add("POST /chaos x3");

            // Slow endpoints — spread of delays for slow request detection
            for (var i = 0; i < 5; i++)
            {
                await http.GetAsync("/slow");
            }
            results.Add("GET /slow x5");

            // Burst: 20 rapid orders in parallel
            var burst = Enumerable.Range(1, 20).Select(i =>
                http.PostAsJsonAsync("/orders", new { productId = Random.Shared.Next(1, 6), quantity = 1, userId = $"burst-user-{i}" }));
            await Task.WhenAll(burst);
            results.Add("POST /orders x20 (burst)");

            logger.LogInformation("Traffic generation complete — {RequestCount} requests sent", results.Count + 20);
            return Results.Ok(new { message = "Traffic generated", requests = results });
        });
    }
}
