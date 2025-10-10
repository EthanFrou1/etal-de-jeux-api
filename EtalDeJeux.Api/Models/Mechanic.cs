using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Mechanic
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = default!;

    public List<ProductMechanic> ProductMechanics { get; set; } = new List<ProductMechanic>();
}
