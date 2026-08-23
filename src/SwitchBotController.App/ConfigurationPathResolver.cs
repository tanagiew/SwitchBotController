namespace SwitchBotController.App.Configuration;

public static class ConfigurationPathResolver
{
    private const string ConfigFileName = "config.json";
    private const string OverrideEnvironmentVariable = "SWITCHBOT_CONFIG_PATH";

    public static string SettingsFilePath
    {
        get
        {
            try
            {
                return Path.Combine(
                    Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                    "settings.json");
            }
            catch (InvalidOperationException)
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SwitchBotController",
                    "settings.json");
            }
        }
    }

    public static string Resolve()
    {
        var overridePath = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var candidates = new List<string>
        {
            Path.Combine(Environment.CurrentDirectory, ConfigFileName),
            Path.Combine(AppContext.BaseDirectory, ConfigFileName)
        };

#if DEBUG
        candidates.Add(Path.GetFullPath(Path.Combine(
            Environment.CurrentDirectory,
            "..",
            "..",
            ConfigFileName)));
#endif

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }
}
