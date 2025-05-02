using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Web3;
using Rentality.Scheduler.API.Models;
using Rentality.Scheduler.API.Utils;

namespace Rentality.Scheduler.API.Services;

internal class RentalityBatchPriceUpdater
{
    private readonly Web3 _web3;
    private readonly ILogger<RentalityBatchPriceUpdater> _logger;
    private readonly string _batchUpdaterAbi;
    private readonly string _batchUpdaterAddress;
    private readonly string _walletPrivateKey;

    public RentalityBatchPriceUpdater(Web3 web3, EnvReader envReader, ILogger<RentalityBatchPriceUpdater> logger)
    {
        _web3 = web3;
        _logger = logger; 
        _batchUpdaterAddress = envReader.GetEnvString("RENTALITY_BATCH_UPDATER_ADDRESS");
        _walletPrivateKey = envReader.GetEnvString("WALLET_PRIVATE_KEY");
        _batchUpdaterAbi = File.ReadAllText("Abis/batch_price_updater.abi.json");

        if (String.IsNullOrWhiteSpace(_batchUpdaterAbi))
        {
            logger.LogError("batchUpdaterAbi was not found or is empty!");
            throw new ArgumentException("batchUpdaterAbi was not found or is empty!");
        }
    }

    public async Task<string> UpdarePrices (List<OracleUpdate> updateRequest)
    {

        var rentalityBatchUpdaterContract = _web3.Eth.GetContract(_batchUpdaterAbi, _batchUpdaterAddress);
        var updatePricesFunction = rentalityBatchUpdaterContract.GetFunction("updatePrices");
        var account = new Nethereum.Web3.Accounts.Account(_walletPrivateKey);

        var estimatedGas = await updatePricesFunction.EstimateGasAsync(account.Address, null, null, updateRequest);
        var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
        var maxPriorityFeePerGas = Web3.Convert.ToWei(5, UnitConversion.EthUnit.Mwei);
        var maxFeePerGas = Web3.Convert.ToWei(25, UnitConversion.EthUnit.Mwei);
        _logger.LogInformation($"estimatedGas: {estimatedGas}, gasPrice: {gasPrice}");

        return await updatePricesFunction.SendTransactionAsync(
            from: account.Address, 
            gas: new HexBigInteger(estimatedGas),
            value: null, 
            maxFeePerGas: new HexBigInteger(maxFeePerGas),
            maxPriorityFeePerGas: new HexBigInteger(maxPriorityFeePerGas),
            functionInput: updateRequest);
    }
}
