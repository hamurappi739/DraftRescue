using DraftRescue.Application.Security;
using DraftRescue.Domain.Drafts;
using Xunit;

namespace DraftRescue.Tests.Architecture;

public sealed class CoreDependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceAvaloniaOrWindowsPlatformAssembly()
    {
        var references = typeof(DraftId).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null)
            .ToArray();

        Assert.DoesNotContain(references, x => x!.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain("DraftRescue.Platform.Windows", references);
        Assert.DoesNotContain("DraftRescue.Infrastructure", references);
    }

    [Fact]
    public void Application_DoesNotReferenceAvaloniaWindowsPlatformOrInfrastructure()
    {
        var references = typeof(CaptureEligibility).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null)
            .ToArray();

        Assert.DoesNotContain(references, x => x!.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain("DraftRescue.Platform.Windows", references);
        Assert.DoesNotContain("DraftRescue.Infrastructure", references);
    }
}
