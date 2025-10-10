namespace EtalDeJeux.Api.Models;

public class ProductPublisher
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = default!;

    public long PublisherId { get; set; }

    public Publisher Publisher { get; set; } = default!;
}
