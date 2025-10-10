using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Publisher
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    public string? Url { get; set; }

    public List<ProductPublisher> ProductPublishers { get; set; } = new List<ProductPublisher>();
}
