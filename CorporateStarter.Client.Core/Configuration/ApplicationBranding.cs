namespace CorporateStarter.Client.Core.Configuration;

public sealed class ApplicationBranding
{
    public string Name { get; private set; } = "CorporateStarter";
    public event Action? Changed;
    public void Apply(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name.Any(char.IsControl))
            throw new InvalidDataException("Invalid application name.");
        Name = name.Trim();
        Changed?.Invoke();
    }
}
