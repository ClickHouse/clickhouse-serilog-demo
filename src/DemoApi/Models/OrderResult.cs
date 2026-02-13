namespace DemoApi.Models;

public sealed class OrderResult
{
    public required string OrderId { get; init; }
    public required int ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string Status { get; init; }
}
