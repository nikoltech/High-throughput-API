namespace HApi.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public record OrderSummary(int Id, int CustomerId, decimal TotalAmount, string Status);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
