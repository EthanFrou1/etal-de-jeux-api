using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class ProductFile
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    [Required, MaxLength(140)]
    public string? Name { get; set; }

    [Required]
    public string? Url { get; set; }

    [MaxLength(40)]
    [Column(TypeName = "text")]
    public string? Kind { get; set; }

    public bool IsPublic { get; set; }
}
