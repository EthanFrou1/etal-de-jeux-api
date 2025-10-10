using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using EtalDeJeux.Api.Serialization;

namespace EtalDeJeux.Api.Models;

[JsonConverter(typeof(EnumMemberJsonConverter<FileKind>))]
public enum FileKind
{
    [EnumMember(Value = "pdf")]
    Pdf,

    [EnumMember(Value = "image")]
    Image,

    [EnumMember(Value = "zip")]
    Zip,

    [EnumMember(Value = "rules")]
    Rules,

    [EnumMember(Value = "scenario")]
    Scenario
}
