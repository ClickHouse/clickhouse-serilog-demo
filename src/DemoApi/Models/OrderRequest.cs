namespace DemoApi.Models;

public sealed class OrderRequest
{
    public required int ProductId { get; init; }
    public required int Quantity { get; init; }
    public required string UserId { get; init; }
}
