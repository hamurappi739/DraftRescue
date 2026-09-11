using Uia = System.Windows.Automation;
using Forms = System.Windows.Forms;

namespace DraftRescue.Platform.Windows.Testing;

public sealed record Phase2NativeFieldMetadata(
    string SurfaceId,
    bool IsPassword,
    bool IsReadOnly,
    bool IsEnabled,
    bool IsKeyboardFocusable,
    int ControlTypeId,
    bool MetadataReadFailed);

/// <summary>
/// Test-only Windows Forms fixture. It reads structural UIA properties only;
/// no Text/Value/Name property is accessed.
/// </summary>
public static class Phase2NativeSecureFixture
{
    public static async Task<IReadOnlyList<Phase2NativeFieldMetadata>> CaptureAsync(
        CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<IReadOnlyList<Phase2NativeFieldMetadata>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => RunFixture(completion))
        {
            IsBackground = true,
            Name = "DraftRescue-Phase2-NativeFixture"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        return await completion.Task.ConfigureAwait(false);
    }

    private static void RunFixture(TaskCompletionSource<IReadOnlyList<Phase2NativeFieldMetadata>> completion)
    {
        try
        {
            using var form = new Forms.Form
            {
                ShowInTaskbar = false,
                Width = 420,
                Height = 220,
                StartPosition = Forms.FormStartPosition.CenterScreen
            };
            var ordinary = new Forms.TextBox { Left = 24, Top = 20, Width = 340 };
            var password = new Forms.TextBox { Left = 24, Top = 64, Width = 340, UseSystemPasswordChar = true };
            var readOnly = new Forms.TextBox { Left = 24, Top = 108, Width = 340, ReadOnly = true };
            form.Controls.AddRange(new Forms.Control[] { ordinary, password, readOnly });
            form.Shown += (_, _) =>
            {
                var fields = new[]
                {
                    ReadMetadata("ordinary-edit", ordinary),
                    ReadMetadata("password-edit", password),
                    ReadMetadata("readonly-edit", readOnly)
                };
                completion.TrySetResult(fields);
                form.Close();
            };
            Forms.Application.Run(form);
        }
        catch
        {
            completion.TrySetResult(Array.Empty<Phase2NativeFieldMetadata>());
        }
    }

    private static Phase2NativeFieldMetadata ReadMetadata(string surfaceId, Forms.Control control)
    {
        try
        {
            var element = Uia.AutomationElement.FromHandle(control.Handle);
            var valuePattern = element.TryGetCurrentPattern(Uia.ValuePattern.Pattern, out var pattern)
                ? (Uia.ValuePattern)pattern
                : null;
            return new Phase2NativeFieldMetadata(
                surfaceId,
                element.Current.IsPassword,
                valuePattern?.Current.IsReadOnly ?? false,
                element.Current.IsEnabled,
                element.Current.IsKeyboardFocusable,
                element.Current.ControlType.Id,
                false);
        }
        catch
        {
            return new Phase2NativeFieldMetadata(surfaceId, false, false, false, false, 0, true);
        }
    }
}
