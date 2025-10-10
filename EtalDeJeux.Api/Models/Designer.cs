using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Designer
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    public List<ProductDesigner> ProductDesigners { get; set; } = new List<ProductDesigner>();
}
