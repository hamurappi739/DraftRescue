using DraftRescue.Application.Models;
using DraftRescue.Platform.Windows.Security;
using Xunit;

namespace DraftRescue.Tests.Observation;

public sealed class ProcessIntegrityCompatibilityReaderTests
{
    [Fact]
    public void CurrentProcess_IsCompatibleWithItself()
    {
        var processId = Environment.ProcessId;

        Assert.Equal(
            IntegrityCompatibility.Compatible,
            ProcessIntegrityCompatibilityReader.Compare(processId, processId));
    }

    [Fact]
    public void InvalidProcess_IsUnknown()
    {
        Assert.Equal(
            IntegrityCompatibility.Unknown,
            ProcessIntegrityCompatibilityReader.Compare(-1, Environment.ProcessId));
    }
}
