using Nethereum.Web3;
using Rentality.Scheduler.API.Models;

namespace Rentality.Scheduler.API.Services;

internal class PriceAggregatorService
{
    private readonly Web3 _web3;
    private readonly ILogger<PriceAggregatorService> _logger;
    private readonly string _aggregatorAbi;

    public PriceAggregatorService(Web3 web3, ILogger<PriceAggregatorService> logger)
    {
        _web3 = web3;
        _logger = logger;
        _aggregatorAbi = File.ReadAllText("Abis/aggregator.abi.json");

        if (String.IsNullOrWhiteSpace(_aggregatorAbi))
        {
            _logger.LogError("aggregatorAbi was not found or is empty!");
            throw new ArgumentException("aggregatorAbi was not found or is empty!");
        }
    }

    public async Task<LatestRoundData> GetLatestAggregatorData(string priceFeedAddress)
    {
        var aggregatorContract = _web3.Eth.GetContract(_aggregatorAbi, priceFeedAddress);
        var latestRoundDataFunction = aggregatorContract.GetFunction("latestRoundData");
        var latestData = await latestRoundDataFunction.CallDeserializingToObjectAsync<LatestRoundData>();

        return latestData;
    }
}
