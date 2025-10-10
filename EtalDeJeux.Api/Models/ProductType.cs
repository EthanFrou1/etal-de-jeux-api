using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EtalDeJeux.Api.Models;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ProductType
{
    [EnumMember(Value = "board_game")]
    BoardGame,

    [EnumMember(Value = "expansion")]
    Expansion,

    [EnumMember(Value = "rpg")]
    Rpg,

    [EnumMember(Value = "accessory")]
    Accessory,

    [EnumMember(Value = "digital")]
    Digital
}
