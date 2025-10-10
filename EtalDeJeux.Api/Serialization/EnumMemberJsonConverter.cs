using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtalDeJeux.Api.Serialization;

/// <summary>
/// A JSON converter that serializes and deserializes enums using their <see cref="EnumMemberAttribute"/> values when present.
/// </summary>
/// <typeparam name="TEnum">The enum type handled by the converter.</typeparam>
public sealed class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Type EnumType = typeof(TEnum);

    private static readonly Dictionary<string, TEnum> StringToEnumMap =
        EnumType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => new
            {
                Value = (TEnum)field.GetValue(null)!,
                Name = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name
            })
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<TEnum, string> EnumToStringMap =
        StringToEnumMap.ToDictionary(pair => pair.Value, pair => pair.Key);

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var enumText = reader.GetString();
            if (enumText != null)
            {
                if (StringToEnumMap.TryGetValue(enumText, out var enumValue))
                {
                    return enumValue;
                }

                if (Enum.TryParse(enumText, ignoreCase: true, out TEnum parsedValue))
                {
                    return parsedValue;
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numericValue))
        {
            if (Enum.IsDefined(EnumType, numericValue))
            {
                return (TEnum)Enum.ToObject(EnumType, numericValue);
            }
        }

        throw new JsonException($"Value '{reader.GetString()}' is not valid for enum type '{EnumType.Name}'.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (EnumToStringMap.TryGetValue(value, out var stringValue))
        {
            writer.WriteStringValue(stringValue);
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
