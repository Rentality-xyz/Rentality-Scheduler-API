using Nethereum.Web3;
using DotNetEnv; 
using Rentality.PriceUpdater.Models;
using Rentality.PriceUpdater.Services;

namespace Rentality.PriceUpdater;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private Web3 _web3;
    private int _chainId = 0;
    private string _walletPrivateKey = "";
    private string _providerApiUrl = "";
    private string _opBnbBnbToUsdPriceFeedAddress = "";
    private string _opBnbUsdtToUsdPriceFeedAddress = "";
    private string _rentalityBatchUpdaterAddress = "";
    private string _rentalityBnbToUsdPriceFeedAddress = "";
    private string _rentalityUsdtToUsdPriceFeedAddress = "";
    private string _aggregatorAbi = "";
    private string _batchUpdaterAbi = "";

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        LoadConfiguration();
        _web3 = new Web3(new Nethereum.Web3.Accounts.Account(_walletPrivateKey, _chainId), _providerApiUrl);
    }

    private void LoadConfiguration()
    {
        if (File.Exists(".env"))
        {
            Env.Load();
        }

        string? chainId = Environment.GetEnvironmentVariable("CHAIN_ID");
        string? walletPrivateKey = Environment.GetEnvironmentVariable("WALLET_PRIVATE_KEY");
        string? providerApiUrl = Environment.GetEnvironmentVariable("PROVIDER_API_URL_5611");
        string? opBnbBnbToUsdPriceFeedAddress = Environment.GetEnvironmentVariable("OPBNB_BNB_USD_PRICE_FEED_ADDRESS");
        string? opBnbUsdtToUsdPriceFeedAddress = Environment.GetEnvironmentVariable("OPBNB_UTSD_USD_PRICE_FEED_ADDRESS");
        string? rentalityBatchUpdaterAddress = Environment.GetEnvironmentVariable("RENTALITY_BATCH_UPDATER_ADDRESS");
        string? rentalityBnbToUsdPriceFeedAddress = Environment.GetEnvironmentVariable("RENTALITY_BNB_USD_PRICE_FEED_ADDRESS");
        string? rentalityUsdtToUsdPriceFeedAddress = Environment.GetEnvironmentVariable("RENTALITY_UTSD_USD_PRICE_FEED_ADDRESS");
        string aggregatorAbi = File.ReadAllText("Abis/aggregator.abi.json");
        string batchUpdaterAbi = File.ReadAllText("Abis/batch_price_updater.abi.json");

        if (String.IsNullOrWhiteSpace(chainId) || !Int32.TryParse(chainId, out _chainId))
        {
            _logger.LogError("CHAIN_ID was not found or is empty or is not integer!");
            throw new ArgumentException("CHAIN_ID was not found or is empty or is not integer!");
        }

        if (String.IsNullOrWhiteSpace(walletPrivateKey))
        {
            _logger.LogError("WALLET_PRIVATE_KEY was not found or is empty!");
            throw new ArgumentException("WALLET_PRIVATE_KEY was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(providerApiUrl))
        {
            _logger.LogError("PROVIDER_API_URL was not found or is empty!");
            throw new ArgumentException("PROVIDER_API_URL was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(opBnbBnbToUsdPriceFeedAddress))
        {
            _logger.LogError("OPBNB_BNB_USD_PRICE_FEED_ADDRESS was not found or is empty!");
            throw new ArgumentException("OPBNB_BNB_USD_PRICE_FEED_ADDRESS was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(opBnbUsdtToUsdPriceFeedAddress))
        {
            _logger.LogError("OPBNB_UTSD_USD_PRICE_FEED_ADDRESS was not found or is empty!");
            throw new ArgumentException("OPBNB_UTSD_USD_PRICE_FEED_ADDRESS was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(rentalityBatchUpdaterAddress))
        {
            _logger.LogError("RENTALITY_BATCH_UPDATER_ADDRESS was not found or is empty!");
            throw new ArgumentException("RENTALITY_BATCH_UPDATER_ADDRESS was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(rentalityBnbToUsdPriceFeedAddress))
        {
            _logger.LogError("RENTALITY_BNB_USD_PRICE_FEED_ADDRESS was not found or is empty!");
            throw new ArgumentException("RENTALITY_BNB_USD_PRICE_FEED_ADDRESS was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(rentalityUsdtToUsdPriceFeedAddress))
        {
            _logger.LogError("RENTALITY_UTSD_USD_PRICE_FEED_ADDRESS was not found or is empty!");
            throw new ArgumentException("RENTALITY_UTSD_USD_PRICE_FEED_ADDRESS was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(aggregatorAbi))
        {
            _logger.LogError("aggregatorAbi was not found or is empty!");
            throw new ArgumentException("aggregatorAbi was not found or is empty!");
        }
        if (String.IsNullOrWhiteSpace(batchUpdaterAbi))
        {
            _logger.LogError("batchUpdaterAbi was not found or is empty!");
            throw new ArgumentException("batchUpdaterAbi was not found or is empty!");
        }

        _walletPrivateKey = walletPrivateKey;
        _providerApiUrl = providerApiUrl;
        _opBnbBnbToUsdPriceFeedAddress = opBnbBnbToUsdPriceFeedAddress;
        _opBnbUsdtToUsdPriceFeedAddress = opBnbUsdtToUsdPriceFeedAddress;
        _rentalityBatchUpdaterAddress = rentalityBatchUpdaterAddress;
        _rentalityBnbToUsdPriceFeedAddress = rentalityBnbToUsdPriceFeedAddress;
        _rentalityUsdtToUsdPriceFeedAddress = rentalityUsdtToUsdPriceFeedAddress;
        _aggregatorAbi = aggregatorAbi;
        _batchUpdaterAbi = batchUpdaterAbi;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var priceAggregator = new PriceAggregatorService(_web3, _aggregatorAbi);
        var batchPriceUpdater = new RentalityBatchPriceUpdater(_web3, _batchUpdaterAbi, _rentalityBatchUpdaterAddress, _walletPrivateKey);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching latest round data...");
                LatestRoundData bnbToUsdLatestData = await priceAggregator.GetLatestAggregatorData(_opBnbBnbToUsdPriceFeedAddress);
                LatestRoundData usdtToUsdLatestData = await priceAggregator.GetLatestAggregatorData(_opBnbUsdtToUsdPriceFeedAddress);

                _logger.LogInformation($"Latest price: BNB/USD: {bnbToUsdLatestData.Answer}, USDT/USD: {usdtToUsdLatestData.Answer}");

                var oracleUpdates = new List<OracleUpdate>
                    {
                        new OracleUpdate { Feed = _rentalityBnbToUsdPriceFeedAddress, Answer = bnbToUsdLatestData.Answer },
                        new OracleUpdate { Feed = _rentalityUsdtToUsdPriceFeedAddress, Answer = usdtToUsdLatestData.Answer }
                    };

                _logger.LogInformation("Updating batch prices...");
                var txHash = await batchPriceUpdater.UpdarePrices(oracleUpdates);
                _logger.LogInformation($"Transaction sent: {txHash}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing blockchain data.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }    
}
