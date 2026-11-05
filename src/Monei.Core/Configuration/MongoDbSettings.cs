namespace Monei.Core.Configuration;

public sealed class MongoDbSettings
{
    /// <summary>Key of the configuration section these settings are bound from.</summary>
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;
}
