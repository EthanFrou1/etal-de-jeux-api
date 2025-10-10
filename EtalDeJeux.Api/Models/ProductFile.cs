using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class ProductFile
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = default!;

    [Required, MaxLength(140)]
    public string Name { get; set; } = default!;

    [Required]
    public string Url { get; set; } = default!;

    [Column(TypeName = "file_kind")]
    public FileKind Kind { get; set; } = FileKind.Pdf;

    public bool IsPublic { get; set; } = true;
}
