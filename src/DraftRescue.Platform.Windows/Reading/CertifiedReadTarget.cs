using System.Windows.Automation;
using DraftRescue.Application.Security;

namespace DraftRescue.Platform.Windows.Reading;

/// <summary>
/// Structural binding resolved after a capability claim. Snapshot sequence is
/// assigned before the provider read; no target text is stored here.
/// </summary>
public sealed record CertifiedReadTarget(
    AutomationElement Element,
    ulong ContextGeneration,
    ulong SnapshotSequence,
    string ProfileId,
    bool IsTextPatternCertified);

public interface ICertifiedReadTargetResolver
{
    bool TryResolve(AllowedFieldHandle handle, out CertifiedReadTarget target);
    bool IsCurrent(CertifiedReadTarget target);
}

public interface ITextPatternDocumentAccessor
{
    Task<string> ReadDocumentAsync(
        AutomationElement element,
        int maximumLength,
        CancellationToken cancellationToken);
}

public sealed class TextPatternUnavailableException : Exception
{
}

/// <summary>
/// Windows UI Automation adapter for certified document surfaces. It requests
/// a finite maximum length and never falls back to ValuePattern, LegacyIAccessible,
/// keyboard input or clipboard data.
/// </summary>
public sealed class UiaTextPatternDocumentAccessor : ITextPatternDocumentAccessor
{
    public Task<string> ReadDocumentAsync(
        AutomationElement element,
        int maximumLength,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (maximumLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }

        return Task.Run(() =>
        {
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
            {
                throw new TextPatternUnavailableException();
            }

            var textPattern = (TextPattern)pattern;
            var documentRange = textPattern.DocumentRange;
            return documentRange.GetText(maximumLength);
        }, cancellationToken);
    }
}
