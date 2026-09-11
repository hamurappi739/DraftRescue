using DraftRescue.Application.Security;
using Xunit;

namespace DraftRescue.Tests.Architecture;

public sealed class Phase2SecurityArchitectureTests
{
    [Fact]
    public void SecurityAssemblyHasNoPlatformOrStorageReference()
    {
        var references = typeof(SecurityPolicyEvaluator).Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null)
            .ToArray();

        Assert.DoesNotContain("DraftRescue.Platform.Windows", references);
        Assert.DoesNotContain("DraftRescue.Infrastructure", references);
        Assert.DoesNotContain(references, x => x!.StartsWith("Avalonia", StringComparison.Ordinal));
    }

    [Fact]
    public void Phase2SecurityConstructorsExcludeReaderAndStorageDependencies()
    {
        var forbidden = new[]
        {
            "IEligibleFieldTextReader", "IDraftTracker", "IDraftRepository", "IDraftProtector",
            "IClipboardService", "IRestoreAdapter", "IRestoreService"
        };

        var securityTypes = typeof(SecurityPolicyEvaluator).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("DraftRescue.Application.Security", StringComparison.Ordinal) == true);
        foreach (var type in securityTypes)
        {
            foreach (var constructor in type.GetConstructors())
            {
                Assert.DoesNotContain(constructor.GetParameters(), parameter =>
                    forbidden.Contains(parameter.ParameterType.Name, StringComparer.Ordinal));
            }
        }
    }

    [Fact]
    public void NoConcreteContentReaderOrPublicCapabilityConstructorExists()
    {
        var applicationTypes = typeof(SecurityPolicyEvaluator).Assembly.GetTypes();
        Assert.DoesNotContain(applicationTypes, type =>
            type.Name.Contains("EligibleFieldTextReader", StringComparison.Ordinal) && !type.IsInterface);
        Assert.Empty(typeof(AllowedFieldHandle).GetConstructors());
        Assert.Null(typeof(AllowedFieldHandle).GetMethod("Parse"));
        Assert.Null(typeof(AllowedFieldHandle).GetMethod("FromJson"));
    }
}
