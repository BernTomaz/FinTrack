using System.Text.Json.Serialization;

namespace FinTrack.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
public enum TransactionType
{
    Income = 1,
    Expense = 2
}
