using DraftRescue.Platform.Windows.Storage;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4PathProviderTests
{
    [Fact]
    public void ExplicitRootUsesSeparatedDatabaseAndSecretPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "DraftRescue-WP45-paths-" + Guid.NewGuid().ToString("N"));
        var provider = new WindowsLocalDataPathProvider(root);

        Assert.Equal(Path.Combine(Path.GetFullPath(root), "db", "drafts.db"), provider.DatabasePath);
        Assert.Equal(Path.Combine(Path.GetFullPath(root), "state", "installation-key.protected"), provider.InstallationSecretPath);
        Assert.DoesNotContain("..", provider.DatabasePath, StringComparison.Ordinal);
        Assert.DoesNotContain("..", provider.InstallationSecretPath, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderOnlyResolvesPathsAndDoesNotCreateDurableDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), "DraftRescue-WP45-no-create-" + Guid.NewGuid().ToString("N"));
        _ = new WindowsLocalDataPathProvider(root);

        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void DefaultRootIsPerUserLocalApplicationData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            Assert.Throws<InvalidOperationException>(() => new WindowsLocalDataPathProvider());
            return;
        }

        var provider = new WindowsLocalDataPathProvider();

        Assert.StartsWith(Path.Combine(localAppData, "DraftRescueData"), provider.DatabasePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine("DraftRescueData", "db"), provider.DatabasePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine("DraftRescueData", "state"), provider.InstallationSecretPath, StringComparison.OrdinalIgnoreCase);
    }
}
