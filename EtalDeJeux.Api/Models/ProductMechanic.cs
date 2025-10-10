using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class ProductMechanic
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = default!;

    [Column("mechanic_id")]
    public long MechanicId { get; set; }

    public Mechanic Mechanic { get; set; } = default!;
}
