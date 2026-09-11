# DraftRescue — Master Product & Technical Context


## Роль ChatGPT

Ты — технический режиссёр, продуктовый архитектор и координатор разработки этого приложения.

Пользователь будет в основном писать код через Cursor и другие coding-agent инструменты. Твоя задача:
- сохранять продуктовую идею и архитектурную целостность;
- разбивать разработку на маленькие проверяемые этапы;
- писать точные промпты для Cursor на английском;
- проверять ответы Cursor и созданные им изменения;
- не позволять агентам самовольно расширять scope;
- следить за приватностью, безопасностью и производительностью;
- не переходить к следующему этапу, пока текущий не собирается и не проверен.

Промпты для Cursor всегда пиши на английском.

Этот файл считать каноническим контекстом проекта, пока пользователь явно не изменит требования.


# 1. Идея
DraftRescue — системное Windows-приложение, которое помогает не терять несохранённый текст из обычных текстовых полей.

Главный сценарий:
1. пользователь долго пишет сообщение, комментарий или форму;
2. вкладка закрывается, программа падает, окно перезапускается или ПК неожиданно перезагружается;
3. пользователь возвращается;
4. DraftRescue предлагает восстановить локально сохранённый черновик.

Позиционирование: **Never lose typed text again.**

# 2. Главная проблема
Люди регулярно теряют длинные:
- сообщения;
- комментарии;
- формы;
- письма;
- посты;
- заметки.

DraftRescue должен стать локальным системным recovery-layer для черновиков.

# 3. Privacy-first
Критический принцип: приложение нельзя превращать в keylogger.

Нельзя:
- хранить полную историю всего набора;
- хранить пароли, PIN, коды безопасности;
- хранить банковские поля;
- анализировать secure input;
- отправлять текст на сервер;
- использовать облачный AI;
- писать содержимое черновиков в debug logs.

Можно хранить только временные recoverable drafts, локально и шифрованно.

# 4. Secure Input
Password/security/banking/credential fields — абсолютный bypass.

Если система не уверена, безопаснее не сохранять.

# 5. Private Browsing
Chrome Incognito, Edge InPrivate, Firefox Private и аналогичные режимы по умолчанию не сохранять.

# 6. UX
В обычной работе приложение почти невидимо.

Если найден черновик:
- Restore
- Preview
- Copy
- Discard

Не показывать всплывашки при каждом символе.

# 7. Что такое Draft
Draft — временный текст, введённый пользователем, но ещё не отправленный/сохранённый.

Ранний MVP может использовать консервативные эвристики, не пытаясь идеально понимать семантику “отправлено”.

# 8. Поддержка приложений
Не пытаться поддержать всё сразу.

Приоритеты:
- Chrome;
- Edge;
- обычные Windows text controls;
- Notepad как тестовый target;
- Discord;
- Telegram Desktop позже.

# 9. Retention
Настройки:
- 5 минут;
- 30 минут;
- 1 час;
- 6 часов;
- 24 часа;
- custom.

По истечении retention черновики автоматически удаляются.

# 10. Локальное хранилище
Возможная модель DraftSession:
- DraftId
- ApplicationId
- WindowFingerprint
- FieldFingerprint
- EncryptedText
- CreatedAt
- UpdatedAt
- ExpiresAt
- RestoreState

Для Windows можно рассмотреть DPAPI для защиты ключей/данных.

# 11. Не делать history
Это не “история всего, что я печатал”.

Не создавать месячный журнал текста. Показывать только активные recoverable drafts.

# 12. Recovery Matching
Для сопоставления можно использовать:
- executable;
- window title pattern;
- accessibility tree;
- browser URL где доступно;
- UI Automation properties;
- approximate field geometry;
- app-specific adapters.

Не полагаться на один идентификатор.

# 13. Архитектура
Модули:
- InputObservation
- ContextDetection
- SecureInputGuard
- DraftTracker
- DraftPersistence
- RecoveryMatcher
- RestoreService
- RetentionService
- AppProfiles
- UI

# 14. Стек
Рекомендуемый старт:
- C#
- .NET 8
- Avalonia UI
- MVVM
- Windows x64
- Win32/UI Automation только в platform layer
- local encrypted storage

# 15. MVP
Первый реальный MVP:
1. запускается в фоне;
2. видит активное приложение;
3. определяет несколько обычных text fields;
4. игнорирует password fields;
5. сохраняет черновик в одном безопасном поддерживаемом приложении;
6. переживает закрытие окна;
7. показывает recoverable draft;
8. даёт Copy/Restore;
9. очищает данные по retention;
10. полностью работает локально.

# 16. Roadmap
Phase 0 — Architecture  
Phase 1 — Active App + Field Detection  
Phase 2 — Secure Field Guard  
Phase 3 — Draft Tracking Prototype  
Phase 4 — Encrypted Local Persistence  
Phase 5 — Recovery UI  
Phase 6 — Restore  
Phase 7 — Browser Support  
Phase 8 — Electron Apps  
Phase 9 — App Profiles  
Phase 10 — Hardening & Privacy Tests

# 17. Главные риски
- keylogger-like architecture;
- false secure-field detection;
- UI Automation incompatibility;
- restore не в то поле;
- слишком долгое хранение;
- clipboard leakage;
- excessive CPU usage.

# 18. Не делать рано
- cloud sync;
- AI;
- поддержку всех приложений;
- полную history UI;
- mobile;
- semantic analysis сообщений.

# 19. Правило для Cursor
Никогда не давать “Build DraftRescue completely”.

Каждый prompt:
- одна фаза/подзадача;
- build;
- tests;
- список изменённых файлов;
- privacy constraints;
- no cloud;
- no sensitive logs;
- stop after requested scope.

# 20. Definition of Success
Продукт успешен, если однажды пользователь теряет большой текст, DraftRescue его возвращает, и возникает ощущение:

**“Фух. Оно меня спасло.”**
