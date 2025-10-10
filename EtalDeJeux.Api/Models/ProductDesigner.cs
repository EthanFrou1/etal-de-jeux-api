namespace EtalDeJeux.Api.Models;

public class ProductDesigner
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = default!;

    public long DesignerId { get; set; }

    public Designer Designer { get; set; } = default!;
}
