using DraftRescue.Desktop.Persistence;

namespace DraftRescue.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _selectedLanguage = "Русский";
    private bool _isDarkTheme;
    private readonly DesktopPersistenceAvailability _persistenceAvailability;

    public MainWindowViewModel(
        DesktopPersistenceAvailability persistenceAvailability = DesktopPersistenceAvailability.Ready)
    {
        _persistenceAvailability = persistenceAvailability;
    }

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

    public bool IsPersistenceUnavailable => _persistenceAvailability != DesktopPersistenceAvailability.Ready;

    public string PersistenceTitle => IsRussian
        ? "Локальная защита временно недоступна"
        : "Local protection is temporarily unavailable";

    public string PersistenceStatus => IsRussian
        ? _persistenceAvailability switch
        {
            DesktopPersistenceAvailability.Corrupt => "Существующее хранилище не открыто. Новые черновики не сохраняются, чтобы не повредить данные.",
            DesktopPersistenceAvailability.Incompatible => "Версия локального хранилища несовместима. Новые черновики не сохраняются.",
            _ => "Хранилище не запущено. Новые черновики не сохраняются до восстановления доступа."
        }
        : _persistenceAvailability switch
        {
            DesktopPersistenceAvailability.Corrupt => "The existing store could not be opened. New drafts are not saved to protect your data.",
            DesktopPersistenceAvailability.Incompatible => "The local store version is incompatible. New drafts are not saved.",
            _ => "The store is not running. New drafts are not saved until access is restored."
        };

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
        RaisePropertyChanged(nameof(PersistenceTitle));
        RaisePropertyChanged(nameof(PersistenceStatus));
    }
}
