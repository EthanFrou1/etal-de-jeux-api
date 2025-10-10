using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using EtalDeJeux.Api.Serialization;
using NpgsqlTypes;

namespace EtalDeJeux.Api.Models;

[JsonConverter(typeof(EnumMemberJsonConverter<FileKind>))]
public enum FileKind
{
    [EnumMember(Value = "pdf"), PgName("pdf")]
    Pdf,

    [EnumMember(Value = "image"), PgName("image")]
    Image,

    [EnumMember(Value = "zip"), PgName("zip")]
    Zip,

    [EnumMember(Value = "rules"), PgName("rules")]
    Rules,

    [EnumMember(Value = "scenario"), PgName("scenario")]
    Scenario
}
