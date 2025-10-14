using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Contracts.Mapping;

public static class EmailMapping
{
    public static EmailLogDto ToDto(this EmailLog log) =>
        new(log.Id, log.OrderId, log.ToEmail, log.Subject, log.MessageId, log.Status, log.Error, log.CreatedAt);
}
