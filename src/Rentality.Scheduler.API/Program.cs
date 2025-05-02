using DotNetEnv; 
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Rentality.Scheduler.API.Models;
using Rentality.Scheduler.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
if (builder.Environment.IsDevelopment() && File.Exists(".env"))
{
    Env.Load();
}
builder.Services.AddLogging();

builder.Services.AddSingleton<Rentality.Scheduler.API.Utils.EnvReader>();
builder.Services.AddSingleton(serviceProvider =>
{
    var envReader = serviceProvider.GetRequiredService<Rentality.Scheduler.API.Utils.EnvReader>();
    var account = new Account(envReader.GetEnvString("WALLET_PRIVATE_KEY"), envReader.GetEnvInt("CHAIN_ID"));
    return new Web3(account, envReader.GetEnvString("PROVIDER_API_URL_5611"));
}
);
builder.Services.AddScoped<PriceAggregatorService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<RentalityBatchPriceUpdater>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => "Service is alive");

app.MapPost("/update-prices", async (PriceAggregatorService priceAggregator,
      Rentality.Scheduler.API.Utils.EnvReader envReader,
      RentalityBatchPriceUpdater batchPriceUpdater,
      EmailService emailService,
      Web3 web3,
      ILogger<Program> logger) =>
{
    try
    {
        var opBnbBnbToUsdPriceFeedAddress = envReader.GetEnvString("OPBNB_BNB_USD_PRICE_FEED_ADDRESS");
        var opBnbUsdtToUsdPriceFeedAddress = envReader.GetEnvString("OPBNB_UTSD_USD_PRICE_FEED_ADDRESS");
        var rentalityBatchUpdaterAddress = envReader.GetEnvString("RENTALITY_BATCH_UPDATER_ADDRESS");
        var rentalityBnbToUsdPriceFeedAddress = envReader.GetEnvString("RENTALITY_BNB_USD_PRICE_FEED_ADDRESS");
        var rentalityUsdtToUsdPriceFeedAddress = envReader.GetEnvString("RENTALITY_UTSD_USD_PRICE_FEED_ADDRESS");

        logger.LogInformation("Fetching latest round data...");
        LatestRoundData bnbToUsdLatestData = await priceAggregator.GetLatestAggregatorData(opBnbBnbToUsdPriceFeedAddress);
        LatestRoundData usdtToUsdLatestData = await priceAggregator.GetLatestAggregatorData(opBnbUsdtToUsdPriceFeedAddress);
        LatestRoundData rentalityBnbToUsdLatestData = await priceAggregator.GetLatestAggregatorData(rentalityBnbToUsdPriceFeedAddress);
        LatestRoundData rentalityUsdtToUsdLatestData = await priceAggregator.GetLatestAggregatorData(rentalityUsdtToUsdPriceFeedAddress);

        logger.LogInformation($"Latest price: BNB/USD: {bnbToUsdLatestData.Answer}, USDT/USD: {usdtToUsdLatestData.Answer}");
        logger.LogInformation($"Rentality Latest price: BNB/USD: {rentalityBnbToUsdLatestData.Answer}, USDT/USD: {rentalityUsdtToUsdLatestData.Answer}");

        var oracleUpdates = new List<OracleUpdate>
                    {
                        new OracleUpdate { Feed = rentalityBnbToUsdPriceFeedAddress, Answer = bnbToUsdLatestData.Answer },
                        new OracleUpdate { Feed = rentalityUsdtToUsdPriceFeedAddress, Answer = usdtToUsdLatestData.Answer }
                    };

        logger.LogInformation("Updating batch prices...");
        var txHash = await batchPriceUpdater.UpdarePrices(oracleUpdates);
        logger.LogInformation($"Transaction sent: {txHash}");
        return Results.Ok("Prices updated.");
    }
    catch (Exception ex) when (ex.Message.Contains("insufficient funds"))
    {
        logger.LogError("Wallet balance is too low");

        try
        {
            var walletPrivateKey = envReader.GetEnvString("WALLET_PRIVATE_KEY");
            var smtpUser = envReader.GetEnvString("SMTP_USER");
            var emailForNotifications = envReader.GetEnvString("NOTIFICATION_EMAIL");
            var account = new Account(walletPrivateKey);
            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            var balanceEth = Web3.Convert.FromWei(balanceWei);

            await emailService.SendLowBalanceAlert(smtpUser, emailForNotifications, account.Address, balanceEth);
        }
        catch (Exception ex1)
        {
            logger.LogError(ex1, "An error occurred while sending email.");
        }
        return Results.Problem("An error occurred while processing blockchain data.");

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while processing blockchain data.");
        return Results.Problem("An error occurred while processing blockchain data.");
    }

});

app.Run();
