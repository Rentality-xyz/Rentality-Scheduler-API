using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace Rentality.Scheduler.API.Models;

[FunctionOutput]
public record LatestRoundData
{
    [Parameter("uint80", "roundId", 1)]
    public BigInteger RoundId { get; init; }

    [Parameter("int256", "answer", 2)]
    public BigInteger Answer { get; init; }

    [Parameter("uint256", "startedAt", 3)]
    public BigInteger StartedAt { get; init; }

    [Parameter("uint256", "updatedAt", 4)]
    public BigInteger UpdatedAt { get; init; }

    [Parameter("uint80", "answeredInRound", 5)]
    public BigInteger AnsweredInRound { get; init; }
}
