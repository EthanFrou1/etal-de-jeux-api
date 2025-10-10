using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EtalDeJeux.Api.Models;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
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
