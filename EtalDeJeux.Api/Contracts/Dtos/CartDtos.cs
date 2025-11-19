using static EtalDeJeux.Api.Controllers.CheckoutController;

namespace EtalDeJeux.Api.Contracts.Dtos;

// Request pour valider le panier
public record ValidateCartRequest(
    List<CartItemRequest> Items
);

public record CartItemRequest(
    string SkuId,
    int Qty
);

// Response enrichie avec les infos de stock
public record ValidateCartResponse(
    List<EnrichedCartItem> Items,
    decimal Subtotal,
    bool HasStockIssues
);

public record EnrichedCartItem(
    string SkuId,
    string ProductSlug,
    string Name,
    List<string>? ImageUrl,
    decimal UnitPrice,
    int RequestedQty,
    int MaxStock,
    bool InStock,
    bool IsDigital,
    string? StockWarning
);

// DTO pour créer/update un customer
public record CustomerDto(
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country
);