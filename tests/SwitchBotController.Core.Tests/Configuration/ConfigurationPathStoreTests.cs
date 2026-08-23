using SwitchBotController.Core.Configuration;

namespace SwitchBotController.Core.Tests.Configuration;

public sealed class ConfigurationPathStoreTests
{
    [Fact]
    public void Load_ReturnsNull_WhenSettingsDoNotExist()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new ConfigurationPathStore(
            Path.Combine(temporaryDirectory.Path, "settings.json"));

        Assert.Null(store.Load());
    }

    [Fact]
    public async Task SaveAsync_StoresOnlyNormalizedConfigurationPath()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        var configPath = Path.Combine(temporaryDirectory.Path, "folder", "..", "config.json");
        var store = new ConfigurationPathStore(settingsPath);

        await store.SaveAsync(configPath);

        Assert.Equal(Path.GetFullPath(configPath), store.Load());
        var json = await File.ReadAllTextAsync(settingsPath);
        Assert.Contains("config_path", json);
        Assert.DoesNotContain("api_token", json);
        Assert.DoesNotContain("devices", json);
    }

    [Fact]
    public async Task Load_ReturnsNull_WhenSettingsAreInvalidJson()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "not json");

        var store = new ConfigurationPathStore(settingsPath);

        Assert.Null(store.Load());
    }

    [Fact]
    public async Task Load_ReturnsNull_WhenSavedPathIsInvalid()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        await File.WriteAllTextAsync(
            settingsPath,
            "{\"config_path\":\"\\u0000\"}");

        var store = new ConfigurationPathStore(settingsPath);

        Assert.Null(store.Load());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"switchbot-controller-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
