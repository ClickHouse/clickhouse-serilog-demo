using System.Collections.Concurrent;
using DemoApi.Models;

namespace DemoApi.Services;

public sealed class ProductService
{
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private int _nextId = 1;

    public ProductService()
    {
        Seed();
    }

    public IReadOnlyList<Product> GetAll() => _products.Values.ToList();

    public Product? GetById(int id) => _products.GetValueOrDefault(id);

    public Product Create(CreateProductRequest request)
    {
        var id = Interlocked.Increment(ref _nextId);
        var product = new Product
        {
            Id = id,
            Name = request.Name,
            Category = request.Category,
            Price = request.Price,
            Stock = request.Stock,
        };
        _products[id] = product;
        return product;
    }

    private void Seed()
    {
        var products = new[]
        {
            new Product { Id = 1, Name = "Mechanical Keyboard", Category = "Electronics", Price = 149.99m, Stock = 50 },
            new Product { Id = 2, Name = "Wireless Mouse", Category = "Electronics", Price = 49.99m, Stock = 120 },
            new Product { Id = 3, Name = "USB-C Hub", Category = "Accessories", Price = 39.99m, Stock = 3 },
            new Product { Id = 4, Name = "Monitor Stand", Category = "Furniture", Price = 89.99m, Stock = 0 },
            new Product { Id = 5, Name = "Webcam HD", Category = "Electronics", Price = 79.99m, Stock = 25 },
        };

        foreach (var product in products)
        {
            _products[product.Id] = product;
        }

        _nextId = products.Length + 1;
    }
}
