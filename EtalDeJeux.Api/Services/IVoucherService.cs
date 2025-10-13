using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Services
{
    public interface IVoucherService
    {
        Task<Voucher> IssueAsync(Guid orderId, string email, decimal amount, CancellationToken ct);
    }

}