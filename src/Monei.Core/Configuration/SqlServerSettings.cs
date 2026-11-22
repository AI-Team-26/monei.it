namespace Monei.Core.Configuration;

public sealed class SqlServerSettings
{
    /// <summary>Key of the configuration section these settings are bound from.</summary>
    public const string SectionName = "SqlServer";

    public string ConnectionString { get; set; } = string.Empty;
}
