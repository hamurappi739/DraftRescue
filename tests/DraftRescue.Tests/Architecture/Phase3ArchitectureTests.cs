using System.Reflection;
using DraftRescue.Application.Contracts.Drafts;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Drafts;
using DraftRescue.Application.Models;
using Xunit;

namespace DraftRescue.Tests.Architecture;

public sealed class Phase3ArchitectureTests
{
    [Fact]
    public void ApplicationAssemblyHasNoPlatformStorageOrUiReference()
    {
        var references = typeof(IEligibleFieldTextReader).Assembly
            .GetReferencedAssemblies()
            .Select(value => value.Name)
            .Where(value => value is not null)
            .ToArray();

        Assert.DoesNotContain("DraftRescue.Platform.Windows", references);
        Assert.DoesNotContain("DraftRescue.Infrastructure", references);
        Assert.DoesNotContain(references, value => value!.StartsWith("Avalonia", StringComparison.Ordinal));
    }

    [Fact]
    public void ReaderContractAcceptsOnlyCapabilityBudgetAndCancellation()
    {
        var method = typeof(IEligibleFieldTextReader).GetMethod(nameof(IEligibleFieldTextReader.ReadOnceAsync));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Collection(parameters,
            value => Assert.Equal("AllowedFieldHandle", value.ParameterType.Name),
            value => Assert.Equal(typeof(ReadBudget), value.ParameterType),
            value => Assert.Equal(typeof(CancellationToken), value.ParameterType));
        Assert.DoesNotContain(parameters, value => value.ParameterType.Name is "AutomationElement" or "IntPtr" or "nint");
    }

    [Fact]
    public void TrackerHasNoPersistenceDependencyAndSnapshotHasNoPlatformType()
    {
        var constructors = typeof(InMemoryDraftTracker).GetConstructors();
        Assert.DoesNotContain(constructors.SelectMany(value => value.GetParameters()), value =>
            value.ParameterType.Name.Contains("Repository", StringComparison.Ordinal) ||
            value.ParameterType.Name.Contains("Protector", StringComparison.Ordinal));

        var snapshotAssembly = typeof(FieldTextSnapshot).Assembly;
        Assert.DoesNotContain(snapshotAssembly.GetReferencedAssemblies(), value =>
            value.Name is "UIAutomationClient" or "System.Windows.Automation");
        Assert.DoesNotContain(typeof(IDraftTracker).GetMethods(), value =>
            value.Name.Contains("Persist", StringComparison.OrdinalIgnoreCase));
    }
}
