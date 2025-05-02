using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace Rentality.Scheduler.API.Models;

public record class OracleUpdate
{
    [Parameter("address", "feed", 1)]
    public string Feed { get; init; } = string.Empty;

    [Parameter("int256", "answer", 2)]
    public BigInteger Answer { get; init; }
}
