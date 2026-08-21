using System.Text.Json.Serialization;

namespace FinTrack.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AccountType>))]
public enum AccountType
{
    Wallet = 1,
    Checking = 2,
    Savings = 3,
    CreditCard = 4
}
