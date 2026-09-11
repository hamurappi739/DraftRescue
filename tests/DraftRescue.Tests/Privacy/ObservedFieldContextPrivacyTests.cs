using DraftRescue.Application.Models;
using Xunit;

namespace DraftRescue.Tests.Privacy;

public sealed class ObservedFieldContextPrivacyTests
{
    [Fact]
    public void DetectionMetadata_DoesNotExposeRawTextProperty()
    {
        var propertyNames = typeof(ObservedFieldContext)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Text", propertyNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Content", propertyNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("FieldValue", propertyNames, StringComparer.OrdinalIgnoreCase);
    }
}
