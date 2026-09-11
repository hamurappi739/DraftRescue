# Большой prompt для исходной нейронки DraftRescue

Скопируй весь текст ниже и передай его исходной нейронке, которая создавала проект.

---

Ты принимаешь на продолжение реальный проект DraftRescue. Работай как старший инженер и одновременно как инженер по безопасности, тестированию и выпуску. Не ограничивайся советами: самостоятельно читай репозиторий, редактируй файлы, запускай команды, исправляй найденные проблемы, создавай недостающие тесты/скрипты/документы и оставляй проверяемые артефакты.

## 1. Рабочая директория и цель

Рабочая директория проекта:

`D:\РАБОЧИЙ СТОЛ\draftRescue\обновленное`

Это Windows x64 проект на C#/.NET 8 с Avalonia UI и MVVM.

DraftRescue — privacy-first приложение для локального восстановления несохранённого текста. Оно должно обнаруживать подходящие черновики только в поддержанных безопасных полях, временно держать один текущий снимок и сохранять его локально только в зашифрованном виде, чтобы пользователь мог восстановить потерянный текст. Это не кейлоггер и не система полной истории набора.

Твоя задача — сделать максимально большой, но безопасный и проверяемый объём работы за один проход в пределах текущего незакрытого пакета Phase 4 WP4.8. Сначала полностью разберись в фактическом состоянии, затем реализуй всё, что действительно необходимо для закрытия WP4.8, проведи проверки и оставь проект в честном состоянии. Не имитируй успешный результат и не подменяй отсутствие доказательства словом Pass.

## 2. Обязательное чтение перед любыми изменениями

До изменения кода прочитай полностью:

1. `CODEX_START_HERE.md`
2. `AGENTS.md`
3. `docs/CURSOR_HANDOFF_INDEX.md`
4. `docs/DRAFTRESCUE_MASTER_CONTEXT.md`
5. `docs/CURRENT_PROJECT_MODE.md`
6. `docs/CODEX_EXECUTION_GUIDE.md`
7. `docs/ADR_DECISION_SUMMARY_V1.md`
8. `docs/OPEN_DECISIONS.md`
9. `docs/IMPLEMENTATION_WORK_PACKAGES.md`
10. `docs/PHASE_ACCEPTANCE_GATES.md`
11. `docs/TEST_CASE_CATALOG.md`
12. `specs/test-catalog.v1.json`
13. `docs/PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`
14. все документы, которые `PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md` помечает обязательными;
15. `docs/PHASE4_IMPLEMENTATION_BOUNDARY.md` и `docs/PHASE4_EXIT_CRITERIA.md`;
16. `docs/PHASE4_FINAL_EXIT_REVIEW_2026-09-08.md`;
17. `CODEX_HANDOFF_STATUS.json` и актуальные JSON-артефакты в `artifacts/phase4-exit-gate`, `artifacts/phase4-fault-gate`, `artifacts/phase4-recovery-gate`, `artifacts/phase4-sqlite-gate`, `artifacts/phase4-coordinator-gate`.

Не считай старые тексты или первоначальный prompt источником истины. Истина — фактический код, тесты, свежие артефакты и канонические документы.

## 3. Фактическое состояние, от которого нужно отталкиваться

Уже закрыто и не должно быть сломано:

- Phase 0 — проверен Windows/.NET SDK.
- Phase 1 WP-1.1..WP-1.4 — observation plane и соответствующие gates Pass.
- Phase 2 WP-2.1..WP-2.8 — metadata-only security/capability boundary и fault matrix Pass.
- Phase 3 WP-3.1..WP-3.8 — bounded TextPattern/ValuePattern readers, snapshots, current-state tracker, integrated evidence и operational soak Pass.
- Phase 4 WP4.1 — protected-record contracts и canonical SQLite schema Pass.
- Phase 4 WP4.2 — DRP1 envelope, DPAPI CurrentUser boundary и installation secret lifecycle Pass на уровне контрактов/тестов.
- Phase 4 WP4.3 — SQLite bootstrap, DELETE journal, secure_delete ON, synchronous EXTRA, serialized writer, transactional SnapshotSequence и metadata-only listing Pass.
- Phase 4 WP4.4 — protect-before-repository coordinator и metadata-only retention Pass.
- Phase 4 WP4.6 — typed corruption classification, local quarantine, non-destructive migration/fail-closed policy Pass.
- Phase 4 WP4.7 — plaintext-at-rest canary и синтетические crash/lock/disk-full fault tests Pass.
- Последний полный тестовый набор: `148/148`.
- Последняя полная сборка при остановленном Desktop: `0 warnings / 0 errors`.
- OS-level process-kill probe уже подтверждает сохранение старой committed row после убийства процесса в незавершённой транзакции.

Текущий результат WP4.8: `Inconclusive`, а не Pass. Реальные блокеры ровно два:

1. `DPAPI CurrentUser` positive roundtrip не наблюдается в текущем test host/profile: `PHASE4-DPAPI-RUNTIME-PROBE.json` сообщает `DpapiFailure`.
2. Настоящая controlled disk-full/volume-quota certification не доступна в текущей среде: синтетический `BeforeCommit` fault проходит, но это не равно физическому исчерпанию места.

Актуальные команды и артефакты:

- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_exit_gate.ps1`
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\probe_phase4_dpapi_runtime.ps1`
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\probe_phase4_target_environment.ps1`
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_process_kill_probe.ps1`
- `artifacts\phase4-exit-gate\PHASE4-EXIT-GATE.json`
- `artifacts\phase4-exit-gate\PHASE4-DPAPI-RUNTIME-PROBE.json`
- `artifacts\phase4-exit-gate\PHASE4-TARGET-ENVIRONMENT-PROBE.json`
- `artifacts\phase4-exit-gate\PHASE4-PROCESS-KILL-PROBE.json`

## 4. Главный рабочий пакет: WP4.8 blocker resolution

Выполни максимально много из следующего, не выходя за границы Phase 4:

### 4.1 DPAPI CurrentUser

- Разбери исходный код `DraftRescue.Platform.Windows` и изолированный `experiments/DraftRescue.Phase4CrashProbe`.
- Проверь, что production protector использует только DPAPI `CurrentUser`, пустой optional entropy и не имеет LocalMachine/plaintext fallback.
- Проверь аргументы, exit codes, stderr/stdout и обработку ошибок runtime probe.
- Найди причину текущего `DpapiFailure`, если она вызвана кодом, упаковкой, x86/x64, profile loading, ACL, пользовательским контекстом, рабочей директорией или ошибкой harness.
- Исправь только подтверждённую техническую причину. Не ослабляй fail-closed политику и не маскируй failure как success.
- Если проблема обусловлена именно ограничением текущего host/profile и безопасно исправить её в коде нельзя, улучши диагностический probe так, чтобы он однозначно различал кодовую ошибку и недоступность профиля, запиши стабильный структурный failure code и зафиксируй блокер в документации.
- Если среда позволяет, выполни реальный positive roundtrip под тем же Windows user/profile, в котором будет запускаться приложение. Доказательством считается только компилируемый production path и свежий JSON-артефакт с roundtrip success; mock/stub не считается.

### 4.2 Controlled disk-full certification

- Изучи `scripts/probe_phase4_target_environment.ps1`, `scripts/run_phase4_process_kill_probe.ps1`, fault injection и критерии `PHASE4_EXIT_CRITERIA`.
- Проверь, можно ли безопасно добавить отдельную controlled test fixture для quota/virtual disk/ограниченного тома без риска для пользовательских данных и без удаления чужих файлов.
- Разрешён только изолированный временный каталог/том, созданный самим тестом, с явной проверкой абсолютного пути и cleanup.
- Нельзя использовать широкое удаление, форматирование, изменение системных томов, заполнение диска пользователя или рискованную виртуализацию ради зелёного отчёта.
- Если безопасная реальная fixture доступна, реализуй её, добавь тест: commit failure не создаёт plaintext fallback, старая строка остаётся, stale/partial state не принимается, процесс завершается контролируемо, cleanup проходит.
- Если controlled fixture недоступна, не притворяйся, что синтетическая инъекция является физическим disk-full. Усиль harness и документацию так, чтобы ограничение было воспроизводимо и явно помечено как `Inconclusive`/`Pending target certification`.

### 4.3 Итоговый exit gate

- После исправлений запусти restore/build/tests.
- Запусти все релевантные Phase-4 guards и `run_phase4_exit_gate.ps1`.
- Сохрани свежие артефакты с датой/временем, test count, exit codes, blocker list и source-boundary result.
- Не выставляй `Pass`, если хотя бы один обязательный критерий не имеет доказательства.
- Если оба блокера закрыты доказательствами — зафиксируй Phase 4 WP4.8 Pass.
- Если хотя бы один блокер остаётся — оставь честный `Inconclusive`/`Pending`, но максимально улучши код, диагностику, тесты и инструкции для target-environment certification.

## 5. Обязательные privacy/security ограничения

Никогда не нарушай эти правила, даже если это упростит реализацию:

- никаких keyboard hooks, keylogger, global keystroke stream или полной истории набора;
- не читать и не сохранять пароли, PIN, credential/security-code, банковские и другие secure-input данные;
- private/incognito browser context по умолчанию не сохраняется;
- browser forms, Electron/Discord и неизвестные приложения не включать без отдельной сертификации;
- не отправлять текст в сеть, cloud, telemetry, analytics или AI;
- не логировать draft contents, clipboard contents, UIA Name/HelpText/ItemStatus/value/text или сырые exception messages;
- при неопределённости классификации, profile, target version, security state, timeout или provider failure — fail closed;
- classification/security decision должен завершиться до target-content read;
- Phase 4 repository принимает только protected records;
- DPAPI только `CurrentUser`; plaintext fallback запрещён;
- SQLite: canonical schema, `journal_mode=DELETE`, `secure_delete=ON`, `synchronous=EXTRA`, bounded busy timeout, один serialized writer;
- SnapshotSequence проверяется транзакционно; только одна текущая строка на DraftId; revision history запрещён;
- recovery list, startup и retention — metadata-only; не расшифровывать тела для списка;
- corruption/migration — typed fail-closed, local quarantine only, без salvage, auto-drop/recreate, upload или network recovery;
- capture capability — только `ReadSnapshot`, не даёт право на Restore;
- не добавлять Preview, Copy или Restore в Phase 4;
- не сохранять пользовательский текст в диагностических артефактах.

Соблюдай privacy invariants `P-001..P-050` и correctness invariants `C-001..C-050`; для каждого нетривиального изменения укажи релевантные ID и добавь/обнови тесты, где это возможно.

## 6. Что строго запрещено делать в этом проходе

- Не начинать Phase 5 и не включать production Preview/Copy/Restore.
- Не подключать cloud sync, AI, telemetry, analytics, mobile, browser capture, Discord/Electron adapters или broad application support.
- Не переделывать продукт с нуля и не менять принятые ADR молча.
- Не удалять существующие guards, тесты, артефакты или fail-closed ветки ради зелёного результата.
- Не подменять реальную проверку mock-ом и не называть синтетический fault физическим disk-full.
- Не использовать destructive commands против корня проекта, всего диска или неизвестных путей.
- Не спрашивать подтверждение на обычные чтение/сборку/тесты. Остановись и сообщи пользователю только если нужен новый внешний authority, недоступен target environment или действие реально опасно.

## 7. Большой пакет обязательных действий после реализации

Выполни в одном максимально крупном блоке:

1. Инвентаризацию текущих изменений и состояния git без потери пользовательских изменений.
2. Обязательное чтение документов из раздела 2.
3. Кодовый аудит DPAPI/probe/fault/exit-gate границ.
4. Исправление подтверждённых дефектов.
5. Новые или усиленные unit/integration/harness tests.
6. Restore solution.
7. Полную сборку solution на Windows x64/.NET 8.
8. Полный тестовый набор и все релевантные Phase-4 gates.
9. Реальный DPAPI probe и process-kill probe.
10. Controlled disk-full probe только если fixture безопасна и изолирована.
11. Обновление `CODEX_HANDOFF_STATUS.json`, `CODEX_START_HERE.md`, `CODEX_REVIEW_TO_ORIGINAL_AI.md`, `docs/PHASE4_FINAL_EXIT_REVIEW_2026-09-08.md` и иных handoff-документов, которые фактически изменились.
12. Обновление `FOUNDATION_MANIFEST.md`, `CODEX_HANDOFF_MANIFEST.md`, `FOUNDATION_FILE_INVENTORY.txt` и `FOUNDATION_SHA256SUMS.txt`, если добавлены/изменены файлы и это требуется текущей процедурой.
13. Проверку, что JSON валиден, артефакты не содержат plaintext, а checksum/inventory согласованы.
14. Запуск Desktop только если это безопасно и не блокирует сборку; не выдавай ручной UI-тест за автоматический.

Если Desktop или apphost блокирует полный build, корректно останови только принадлежащий проекту Desktop-процесс, повтори build и укажи это в отчёте. Не убивай посторонние процессы.

## 8. Команды, которыми следует пользоваться

Базовый рабочий каталог:

`Set-Location 'D:\РАБОЧИЙ СТОЛ\draftRescue\обновленное'`

Основной exit gate:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_exit_gate.ps1`

DPAPI probe:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\probe_phase4_dpapi_runtime.ps1`

Target environment probe:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\probe_phase4_target_environment.ps1`

Process-kill probe:

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_process_kill_probe.ps1`

Обычные restore/build/test команды выбирай по фактическим `*.sln`, `*.csproj`, `global.json` и существующим скриптам. Не подставляй выдуманные пути или версии SDK. Перед полным build останови только принадлежащий DraftRescue Desktop, если он запущен.

## 9. Формат итогового отчёта

После выполнения не пиши общий рассказ. Верни точный инженерный отчёт:

1. Что было прочитано и какая фаза/WP реально выполнялась.
2. Что изменено — список абсолютных путей и краткое назначение каждого файла.
3. Какие дефекты найдены и как исправлены.
4. Какие тесты/guards/gates запущены: точные команды, exit code, Pass/Fail/Inconclusive и количество тестов.
5. Текущее решение WP4.8: Pass или честный Inconclusive/Pending.
6. Свежие пути к JSON/MD evidence artifacts.
7. Какие блокеры остались и почему их нельзя честно закрыть в этой среде.
8. Какие privacy/correctness invariants покрыты.
9. Какие риски остаются перед production.
10. Что строго делать следующим bounded work package — без самовольного перехода к Phase 5.

Если действительно требуется ручная проверка визуала или пользовательского поведения, напиши отдельной строкой ровно:

`АНДРЕЙ, НУЖЕН ТЕСТ ВИЗУАЛА И ФУНКЦИОНАЛА`

Добавляй эту строку только когда автоматические проверки не могут заменить ручной тест. Для backend, DPAPI, SQLite, guards и fault harness ручной тест не требуй.

Главное: сделай максимально большой реальный объём работы за один проход, но сохрани честность evidence, fail-closed безопасность и границы Phase 4. Не останавливайся на мелком совете — редактируй проект, запускай проверки и оставь после себя воспроизводимый результат.

---
