using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using EtalDeJeux.Api.Serialization;

namespace EtalDeJeux.Api.Models;

[JsonConverter(typeof(EnumMemberJsonConverter<ProductType>))]
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
