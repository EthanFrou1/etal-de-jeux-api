using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Designer
{
    [Key]
    public long Id { get; set; }        // correspond à BIGSERIAL côté DB

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    // navigation inverse N:N
    public List<ProductDesigner> ProductDesigners { get; set; } = new();
}
