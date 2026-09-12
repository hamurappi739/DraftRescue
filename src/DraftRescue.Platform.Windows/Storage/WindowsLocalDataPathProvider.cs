using System.IO;
using DraftRescue.Application.Contracts.Persistence;

namespace DraftRescue.Platform.Windows.Storage;

/// <summary>Resolves DraftRescue's per-user durable paths without creating them.</summary>
public sealed class WindowsLocalDataPathProvider : IAppDataPathProvider
{
    private const string ProductDirectoryName = "DraftRescueData";
    private readonly string _root;

    public WindowsLocalDataPathProvider(string? rootOverride = null)
    {
        _root = string.IsNullOrWhiteSpace(rootOverride)
            ? ResolveDefaultRoot()
            : Path.GetFullPath(rootOverride);
    }

    public string DatabasePath => Path.Combine(_root, "db", "drafts.db");

    public string InstallationSecretPath => Path.Combine(_root, "state", "installation-key.protected");

    private static string ResolveDefaultRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            throw new InvalidOperationException("Per-user local application data path is unavailable.");
        return Path.Combine(localAppData, ProductDirectoryName);
    }
}
