using System.Text.Json.Serialization;

namespace FinTrack.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<CategoryType>))]
public enum CategoryType
{
    Income = 1,
    Expense = 2
}
