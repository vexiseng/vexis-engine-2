namespace Vexis.Foundation;

public static class Guard
{
    public static string NotBlank(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or blank.", parameterName);
        }

        return value.Trim();
    }
}
