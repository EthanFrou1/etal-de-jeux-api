using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(AppDbContext db, ILogger<CustomersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<CustomerDto>> GetByEmail(string email, CancellationToken ct)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), ct);

        if (customer == null)
        {
            return NotFound(new { error = "Client non trouvé" });
        }

        return Ok(new CustomerDto(
            Email: customer.Email,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            Phone: customer.Phone,
            AddressLine1: customer.AddressLine1,
            AddressLine2: customer.AddressLine2,
            City: customer.City,
            PostalCode: customer.PostalCode,
            Country: customer.Country
        ));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateOrUpdate(
        [FromBody] CustomerDto dto,
        CancellationToken ct)
    {
        try
        {
            var existing = await _db.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email.ToLower(), ct);

            if (existing != null)
            {
                // Mise à jour
                existing.FirstName = dto.FirstName;
                existing.LastName = dto.LastName;
                existing.Phone = dto.Phone;
                existing.AddressLine1 = dto.AddressLine1;
                existing.AddressLine2 = dto.AddressLine2;
                existing.City = dto.City;
                existing.PostalCode = dto.PostalCode;
                existing.Country = dto.Country ?? "FR";
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                // Création
                var newCustomer = new Customer
                {
                    IdCustomer = Guid.NewGuid(),
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Phone = dto.Phone,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    Country = dto.Country ?? "FR"
                };

                _db.Customers.Add(newCustomer);
            }

            await _db.SaveChangesAsync(ct);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création/mise à jour du client");
            return BadRequest(new { error = "Erreur lors de l'enregistrement" });
        }
    }
}