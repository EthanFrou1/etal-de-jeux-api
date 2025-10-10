using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using EtalDeJeux.Api.Serialization;
using NpgsqlTypes;

namespace EtalDeJeux.Api.Models;

[JsonConverter(typeof(EnumMemberJsonConverter<ProductType>))]
public enum ProductType
{
    [EnumMember(Value = "board_game"), PgName("board_game")]
    BoardGame,

    [EnumMember(Value = "expansion"), PgName("expansion")]
    Expansion,

    [EnumMember(Value = "rpg"), PgName("rpg")]
    Rpg,

    [EnumMember(Value = "accessory"), PgName("accessory")]
    Accessory,

    [EnumMember(Value = "digital"), PgName("digital")]
    Digital
}
