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
    private string _emailForNotifications = "";
    private string _smtpHost = "";
    private string _smtpUser = "";
    private string _smtpPassword = "";
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

        string? chainIdString = GetEnvString("CHAIN_ID");
        _walletPrivateKey = GetEnvString("WALLET_PRIVATE_KEY");
        _providerApiUrl = GetEnvString("PROVIDER_API_URL_5611");
        _opBnbBnbToUsdPriceFeedAddress = GetEnvString("OPBNB_BNB_USD_PRICE_FEED_ADDRESS");
        _opBnbUsdtToUsdPriceFeedAddress = GetEnvString("OPBNB_UTSD_USD_PRICE_FEED_ADDRESS");
        _rentalityBatchUpdaterAddress = GetEnvString("RENTALITY_BATCH_UPDATER_ADDRESS");
        _rentalityBnbToUsdPriceFeedAddress = GetEnvString("RENTALITY_BNB_USD_PRICE_FEED_ADDRESS");
        _rentalityUsdtToUsdPriceFeedAddress = GetEnvString("RENTALITY_UTSD_USD_PRICE_FEED_ADDRESS");

        _emailForNotifications = GetEnvString("NOTIFICATION_EMAIL");
        _smtpHost = GetEnvString("SMTP_HOST");
        _smtpUser = GetEnvString("SMTP_USER");
        _smtpPassword = GetEnvString("SMTP_PASSWORD");

        string aggregatorAbi = File.ReadAllText("Abis/aggregator.abi.json");
        string batchUpdaterAbi = File.ReadAllText("Abis/batch_price_updater.abi.json");

        if (!Int32.TryParse(chainIdString, out _chainId))
        {
            _logger.LogError("CHAIN_ID is not integer!");
            throw new ArgumentException("CHAIN_ID  is not integer!");
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
          
        _aggregatorAbi = aggregatorAbi;
        _batchUpdaterAbi = batchUpdaterAbi;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var priceAggregator = new PriceAggregatorService(_web3, _aggregatorAbi);
        var batchPriceUpdater = new RentalityBatchPriceUpdater(_web3, _batchUpdaterAbi, _rentalityBatchUpdaterAddress, _walletPrivateKey, _logger);
         
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
            catch (Exception ex) when (ex.Message.Contains("insufficient funds"))
            {
                _logger.LogError("Wallet balance is too low");

                try
                {
                    var account = new Nethereum.Web3.Accounts.Account(_walletPrivateKey);
                    var balanceWei = await _web3.Eth.GetBalance.SendRequestAsync(account.Address);
                    var balanceEth = Web3.Convert.FromWei(balanceWei);
                    var emailService = new EmailService(_smtpHost, _smtpUser, _smtpPassword);

                    await emailService.SendLowBalanceAlert(_smtpUser, _emailForNotifications, account.Address, balanceEth);
                }
                catch (Exception ex1)
                {
                    _logger.LogError(ex1, "An error occurred while sending email.");
                }

            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing blockchain data.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }    

    private string GetEnvString(string envName)
    {
        string? envString = Environment.GetEnvironmentVariable(envName);

        if (String.IsNullOrWhiteSpace(envString))
        {
            _logger.LogError($"{envName} was not found or is empty!");
            throw new ArgumentException($"{envName} was not found or is empty!");
        }

        return envString;
    }
}
