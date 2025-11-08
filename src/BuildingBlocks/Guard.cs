namespace IOC.BuildingBlocks;

public static class Guard
{
    public static GuardAgainst Against => new();

    public static void AgainstNull(object? input, string parameterName)
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    public static void AgainstNullOrWhiteSpace(string? input, string name)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException($"{name} cannot be null or whitespace", name);
        }
    }
}

public class GuardAgainst
{
    public string NullOrWhiteSpace(string? input, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException($"{parameterName} cannot be null or whitespace", parameterName);
        }
        return input!;
    }

    public T Null<T>(T? input, string parameterName)
        where T : class
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return input;
    }

    public double NegativeOrZero(double input, string parameterName)
    {
        if (input <= 0)
        {
            throw new ArgumentException($"{parameterName} must be positive", parameterName);
        }
        return input;
    }

    public int NegativeOrZero(int input, string parameterName)
    {
        if (input <= 0)
        {
            throw new ArgumentException($"{parameterName} must be positive", parameterName);
        }
        return input;
    }

    public T InvalidInput<T>(T input, string parameterName, Func<T, bool> predicate, string message)
    {
        if (predicate(input))
        {
            throw new ArgumentException(message, parameterName);
        }
        return input;
    }
}
