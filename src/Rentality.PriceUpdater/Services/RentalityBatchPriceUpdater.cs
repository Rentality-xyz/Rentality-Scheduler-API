using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Web3;
using Rentality.PriceUpdater.Models;

namespace Rentality.PriceUpdater.Services;

internal class RentalityBatchPriceUpdater(Web3 web3, string batchUpdaterAbi, string batchUpdaterAddress, string walletPrivateKey, ILogger<Worker> logger)
{
    public async Task<string> UpdarePrices (List<OracleUpdate> updateRequest)
    {

        var rentalityBatchUpdaterContract = web3.Eth.GetContract(batchUpdaterAbi, batchUpdaterAddress);
        var updatePricesFunction = rentalityBatchUpdaterContract.GetFunction("updatePrices");
        var account = new Nethereum.Web3.Accounts.Account(walletPrivateKey );

        var estimatedGas = await updatePricesFunction.EstimateGasAsync(account.Address, null, null, updateRequest);
        var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
        var maxPriorityFeePerGas = Web3.Convert.ToWei(5, UnitConversion.EthUnit.Mwei);
        var maxFeePerGas = Web3.Convert.ToWei(25, UnitConversion.EthUnit.Mwei);
        logger.LogInformation($"estimatedGas: {estimatedGas}, gasPrice: {gasPrice}");

        return await updatePricesFunction.SendTransactionAsync(
            from: account.Address, 
            gas: new HexBigInteger(estimatedGas),
            value: null, 
            maxFeePerGas: new HexBigInteger(maxFeePerGas),
            maxPriorityFeePerGas: new HexBigInteger(maxPriorityFeePerGas),
            functionInput: updateRequest);
    }
}
