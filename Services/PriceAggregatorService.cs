using Nethereum.Web3;
using Rentality.PriceUpdater.Models;

namespace Rentality.PriceUpdater.Services;

internal class PriceAggregatorService(Web3 web3, string aggregatorAbi)
{
    public async Task<LatestRoundData> GetLatestAggregatorData(string priceFeedAddress)
    {
        var aggregatorContract = web3.Eth.GetContract(aggregatorAbi, priceFeedAddress);
        var latestRoundDataFunction = aggregatorContract.GetFunction("latestRoundData");
        var latestData = await latestRoundDataFunction.CallDeserializingToObjectAsync<LatestRoundData>();

        return latestData;
    }
}
