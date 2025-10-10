using System.Collections.Generic;
using System.Linq;
using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Contracts;

public record SkuDto(Guid Id, string Name, string SkuCode, decimal Price, string Currency, int? Stock, bool Active, bool IsDefault);

public record ProductContentDto(string Name, int? Quantity);

public record SimpleEntityDto(long Id, string Name);

public record PublisherDto(long Id, string Name, string? Url);

public record ProductFileDto(Guid Id, string Name, string Url, FileKind Kind, bool IsPublic);

public record ProductDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    ProductType Type,
    string? Category,
    bool Active,
    IReadOnlyList<string> Images,
    IReadOnlyList<string> Languages,
    short? ReleaseYear,
    short? MinPlayers,
    short? MaxPlayers,
    short? MinAge,
    short? PlaytimeMin,
    short? PlaytimeMax,
    decimal? Complexity,
    Guid? ParentProductId,
    IReadOnlyList<ProductContentDto> Contents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<SkuDto> Skus,
    IReadOnlyList<SimpleEntityDto> Mechanics,
    IReadOnlyList<SimpleEntityDto> Designers,
    IReadOnlyList<PublisherDto> Publishers,
    IReadOnlyList<ProductFileDto> Files);

public record PaginatedProductsDto(List<ProductDto> Items, int Total, int Page);

public static class DtoMapper
{
    public static ProductDto ToDto(this Product p) =>
        new(
            p.Id,
            p.Slug,
            p.Name,
            p.Description,
            p.Type,
            p.Category,
            p.Active,
            p.Images,
            p.Languages,
            p.ReleaseYear,
            p.MinPlayers,
            p.MaxPlayers,
            p.MinAge,
            p.PlaytimeMin,
            p.PlaytimeMax,
            p.Complexity,
            p.ParentProductId,
            p.Contents.Select(c => new ProductContentDto(c.Name, c.Quantity)).ToList(),
            p.CreatedAt,
            p.UpdatedAt,
            p.Skus
                .OrderByDescending(s => s.IsDefault)
                .ThenBy(s => s.Name)
                .Select(s => new SkuDto(s.Id, s.Name, s.SkuCode, s.Price, s.Currency, s.Stock, s.Active, s.IsDefault))
                .ToList(),
            p.ProductMechanics
                .Select(pm => new SimpleEntityDto(pm.MechanicId, pm.Mechanic.Name))
                .OrderBy(m => m.Name)
                .ToList(),
            p.ProductDesigners
                .Select(pd => new SimpleEntityDto(pd.DesignerId, pd.Designer.Name))
                .OrderBy(d => d.Name)
                .ToList(),
            p.ProductPublishers
                .Select(pp => new PublisherDto(pp.PublisherId, pp.Publisher.Name, pp.Publisher.Url))
                .OrderBy(pub => pub.Name)
                .ToList(),
            p.Files
                .OrderBy(f => f.Name)
                .Select(f => new ProductFileDto(f.Id, f.Name, f.Url, f.Kind, f.IsPublic))
                .ToList()
        );
}
