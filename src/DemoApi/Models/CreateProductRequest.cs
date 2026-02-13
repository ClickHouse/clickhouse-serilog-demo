namespace DemoApi.Models;

public sealed class CreateProductRequest
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required decimal Price { get; init; }
    public required int Stock { get; init; }
}
