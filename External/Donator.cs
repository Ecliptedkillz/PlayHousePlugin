namespace PlayhousePlugin.External;

public sealed class Donator
{
    public string UserId { get; set; } = string.Empty;
    public int Tier { get; set; }
    public bool IsBooster { get; set; }
    public string Preference { get; set; } = string.Empty;
}
