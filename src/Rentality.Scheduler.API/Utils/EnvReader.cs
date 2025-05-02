namespace Rentality.Scheduler.API.Utils;

public class EnvReader(ILogger<EnvReader> logger)
{
    public string GetEnvString(string envName)
    {
        string? envString = Environment.GetEnvironmentVariable(envName);

        if (String.IsNullOrWhiteSpace(envString))
        {
            logger.LogError($"{envName} was not found or is empty!");
            throw new ArgumentException($"{envName} was not found or is empty!");
        }

        return envString;
    }
    public int GetEnvInt(string envName)
    {
        string? envString = Environment.GetEnvironmentVariable(envName);

        if (String.IsNullOrWhiteSpace(envString))
        {
            logger.LogError($"{envName} was not found or is empty!");
            throw new ArgumentException($"{envName} was not found or is empty!");
        }
        if (!Int32.TryParse(envString, out var envInt))
        {
            logger.LogError($"{envName} is not integer!");
            throw new ArgumentException($"{envName} is not integer!");
        }

        return envInt;
    }
}
 
