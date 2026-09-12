# DraftRescue — WP4.8 target certification runbook

Этот runbook закрывает только два оставшихся внешних доказательства Phase 4: DPAPI `CurrentUser` и реальный контролируемый disk-full. Он не включает Preview, Copy, Restore или Phase 5.

## Требования к среде

Запускайте из интерактивной пользовательской сессии Windows под тем же пользователем, под которым будет работать DraftRescue. Профиль пользователя должен быть загружен (`profile.loaded=true`), каталог `%APPDATA%\Microsoft\Protect` должен существовать, а рабочий каталог и временный том должны быть доступны для записи.

Для disk-full нужен заранее подготовленный disposable quota/virtual-disk fixture или отдельная тестовая VM. Не используйте системный диск, рабочую папку пользователя или единственную копию базы. Fixture должен быть создан и размечен инфраструктурой отдельно; данный скрипт ничего не заполняет и не удаляет автоматически.

## Запуск

```powershell
cd "D:\РАБОЧИЙ СТОЛ\draftRescue\обновленное"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_target_certification.ps1
```

Скрипт запускает content-free environment probe, скомпилированный DPAPI probe и process-kill rollback probe. DPAPI probe проверяет оба пути: payload и installation HMAC secret. Он сохраняет только структурные `failureStage` (`Protect`, `Unprotect`, `InstallationSecretCreate`, `InstallationSecretLoad`, `Validation` или `Unknown`), `failureReason` (`PlatformNotSupported`, `Unauthorized`, `Cryptographic` или `Unknown`), два boolean-признака round-trip и объект `readiness` с закрытым списком capability-блокеров; profile state типизирован как `Loaded`, `NotLoaded`, `Unknown` или `QueryUnavailable`, а неизвестное значение даёт `profile-state-unrecognized`. Текста исключения и payload в evidence нет. Это позволяет отличить отказ вызова DPAPI payload, отказ installation-secret, проблему профиля/сессии или ошибку проверки round-trip, не раскрывая содержимое. Artifact privacy guard дополнительно отклоняет неизвестные readiness status/blocker labels и `Pass` с непустым blocker list. Все Phase-4 runners отключают MSBuild node reuse на время запуска и ведут явную границу владения процессами, снижая риск переноса windowless `dotnet`-процессов между сертификациями. После завершения runner удаляет только новые безоконные `dotnet`-процессы, созданные в рамках этого запуска, и записывает `leakedCount`; ожидаемый результат в неподготовленном профиле — `Inconclusive`, а не Pass.

Если инфраструктурная команда получила отдельный JSON от реального disk-full fixture, передайте его явно:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_target_certification.ps1 `
  -DiskFullEvidencePath "D:\certification\PHASE4-DISK-FULL-PROBE.json"
```

Принимается только evidence с явными полями `classification=RealControlledDiskFull`, `outcome=Pass`, `syntheticOnly=false` и `containsSecrets=false`. Отсутствующее поле считается нарушением контракта; синтетический `BeforeCommit` результат не может заменить это доказательство.

Неверный или повреждённый путь не приводит к аварийному завершению: runner создаёт typed `Inconclusive` с `diskFullEvidenceLoadStatus=DiskFullEvidenceNotFound` или `DiskFullEvidenceMalformed` и сохраняет следующий шаг.

## Критерий закрытия

`artifacts\phase4-target-certification\PHASE4-TARGET-CERTIFICATION.json` должен иметь `outcome=Pass`, `phase4ExitReady=true` и пустой `pendingItems`. После этого повторите основной combiner:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase4_exit_gate.ps1
```

Если disk-full evidence хранится вне `artifacts\phase4-exit-gate`, передайте его явно основному combiner через `-DiskFullEvidencePath`; gate проверит обязательные поля и сохранит только безопасное имя артефакта, не раскрывая абсолютный путь.

Только `PHASE4-EXIT-GATE.json` с `outcome=Pass` и `phase4Exit=true` закрывает Phase 4. Любая ошибка профиля, DPAPI, quota/virtual-disk, процесса или доказательства остаётся `Inconclusive`/`Fail` и должна быть устранена на target-среде; plaintext fallback запрещён.
