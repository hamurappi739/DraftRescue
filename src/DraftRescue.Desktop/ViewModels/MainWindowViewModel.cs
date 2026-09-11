namespace DraftRescue.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _selectedLanguage = "Русский";
    private bool _isDarkTheme;

    public event EventHandler? ThemeChanged;

    public IReadOnlyList<string> Languages { get; } = new[] { "Русский", "English" };

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (string.Equals(_selectedLanguage, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedLanguage = value;
            RaisePropertyChanged();
            RaiseLocalizedPropertiesChanged();
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
            {
                return;
            }

            _isDarkTheme = value;
            RaisePropertyChanged();
            RaiseLocalizedPropertiesChanged();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Title => "DraftRescue";

    public string Subtitle => IsRussian ? "Вернуть несохранённый текст" : "Recover unsaved text";

    public string LanguageLabel => IsRussian ? "Язык" : "Language";

    public string ThemeLabel => IsRussian ? "Оформление" : "Appearance";

    public string DarkThemeOn => IsRussian ? "Тёмная" : "Dark";

    public string DarkThemeOff => IsRussian ? "Светлая" : "Light";

    public string Phase => IsRussian ? "Тихая локальная защита черновиков" : "Quiet local draft protection";

    public string SectionTitle => IsRussian ? "Пока нечего восстанавливать" : "Nothing to recover";

    public string Status => IsRussian
        ? "Если поддерживаемый черновик появится после неожиданного закрытия окна, он будет показан здесь."
        : "If a recoverable draft appears after an unexpected window close, it will show up here.";

    public string PrivacySummary => IsRussian
        ? "Черновики остаются на этом компьютере и автоматически истекают. Пароли и защищённые поля не сохраняются."
        : "Drafts stay on this PC and expire automatically. Passwords and secure fields are never saved.";

    public string ScopeNote => IsRussian
        ? "Приложение работает спокойно в фоне и не ведёт историю всего набранного."
        : "DraftRescue stays quiet in the background and does not keep a history of everything you type.";

    private bool IsRussian => string.Equals(SelectedLanguage, "Русский", StringComparison.Ordinal);

    private void RaiseLocalizedPropertiesChanged()
    {
        RaisePropertyChanged(nameof(Subtitle));
        RaisePropertyChanged(nameof(LanguageLabel));
        RaisePropertyChanged(nameof(ThemeLabel));
        RaisePropertyChanged(nameof(DarkThemeOn));
        RaisePropertyChanged(nameof(DarkThemeOff));
        RaisePropertyChanged(nameof(Phase));
        RaisePropertyChanged(nameof(SectionTitle));
        RaisePropertyChanged(nameof(Status));
        RaisePropertyChanged(nameof(PrivacySummary));
        RaisePropertyChanged(nameof(ScopeNote));
    }
}
