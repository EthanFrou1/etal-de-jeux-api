using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly AppDbContext _db;
        public VoucherService(AppDbContext db) => _db = db;

        public async Task<Voucher> IssueAsync(Guid orderId, string email, decimal amount, CancellationToken ct)
        {
            var code = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("=", "").Replace("+", "").Replace("/", "").Substring(0, 16).ToUpperInvariant();

            var v = new Voucher
            {
                Code = code,
                Type = "gift_card",
                Status = "active",
                OrderId = orderId,
                IssuedToEmail = email,
                InitialAmount = amount,
                RemainingAmount = amount
            };
            _db.Vouchers.Add(v);
            await _db.SaveChangesAsync(ct);
            return v;
        }
    }

}
