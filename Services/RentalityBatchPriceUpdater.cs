using Nethereum.Web3;
using Rentality.PriceUpdater.Models;

namespace Rentality.PriceUpdater.Services;

internal class RentalityBatchPriceUpdater(Web3 web3, string batchUpdaterAbi, string batchUpdaterAddress, string walletPrivateKey)
{
    public async Task<string> UpdarePrices (List<OracleUpdate> updateRequest)
    {

        var rentalityBatchUpdaterContract = web3.Eth.GetContract(batchUpdaterAbi, batchUpdaterAddress);
        var updatePricesFunction = rentalityBatchUpdaterContract.GetFunction("updatePrices");

        return await updatePricesFunction.SendTransactionAsync( walletPrivateKey, updateRequest);
    }
}
