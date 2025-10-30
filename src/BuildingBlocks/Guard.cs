namespace IOC.BuildingBlocks;

public static class Guard
{
    public static void AgainstNull(object? input, string name)
    {
        if (input is null)
        {
            throw new ArgumentNullException(name);
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
