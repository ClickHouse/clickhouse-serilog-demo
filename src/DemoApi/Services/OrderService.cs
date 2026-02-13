using DemoApi.Models;

namespace DemoApi.Services;

public sealed class OrderService
{
    private readonly ProductService _products;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ProductService products, ILogger<OrderService> logger)
    {
        _products = products;
        _logger = logger;
    }

    public OrderResult PlaceOrder(OrderRequest request)
    {
        var product = _products.GetById(request.ProductId)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} not found");

        // Out of stock
        if (product.Stock <= 0)
        {
            _logger.LogError(
                "Order failed — product {ProductId} ({ProductName}) is out of stock",
                product.Id, product.Name);

            return new OrderResult
            {
                OrderId = Guid.NewGuid().ToString("N")[..8],
                ProductId = product.Id,
                Quantity = request.Quantity,
                TotalPrice = 0,
                Status = "OutOfStock",
            };
        }

        // Low stock warning
        if (product.Stock <= 5)
        {
            _logger.LogWarning(
                "Low stock alert: product {ProductId} ({ProductName}) has only {Stock} units remaining",
                product.Id, product.Name, product.Stock);
        }

        // Simulate random payment failure (~15% of the time)
        if (Random.Shared.NextDouble() < 0.15)
        {
            _logger.LogError(
                "Payment gateway timeout for user {UserId} ordering product {ProductId}, amount {Amount:C}",
                request.UserId, product.Id, product.Price * request.Quantity);

            return new OrderResult
            {
                OrderId = Guid.NewGuid().ToString("N")[..8],
                ProductId = product.Id,
                Quantity = request.Quantity,
                TotalPrice = product.Price * request.Quantity,
                Status = "PaymentFailed",
            };
        }

        // Success — Interlocked for safe concurrent decrements during burst traffic
        Interlocked.Add(ref product.Stock, -request.Quantity);
        var orderId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "Order {OrderId} placed by {UserId}: {Quantity}x {ProductName} for {TotalPrice:C}",
            orderId, request.UserId, request.Quantity, product.Name, product.Price * request.Quantity);

        return new OrderResult
        {
            OrderId = orderId,
            ProductId = product.Id,
            Quantity = request.Quantity,
            TotalPrice = product.Price * request.Quantity,
            Status = "Confirmed",
        };
    }
}
