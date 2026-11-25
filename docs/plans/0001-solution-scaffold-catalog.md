# План 0001. Каркас решения и сервис Catalog с одним доменным классом

- Статус: утверждён владельцем 2026-11-16 (раннер — вариант C; О2–О5 — рекомендованные ответы, см. раздел 6)
- Дата: 2026-11-16
- Ветка: feat/solution-scaffold-catalog (создаёт основная сессия)
- Связанные решения: ADR 0001 (тестовый фреймворк и раннер), ADR 0002 (ошибки инвариантов домена),
  ADR 0003 (каркас, правила сборки, проверка слоёв). Все три — в статусе «предложено» и утверждаются вместе с этим планом.

Исполнители не видят чат. Всё, что нужно, — в этом файле и в трёх ADR выше.

## 1. Цель и объём

Создать каркас .NET-решения, на который лягут все сервисы, и первый сервис Catalog из четырёх слоёв с одной доменной сущностью
`Product` и эндпоинтом `/health`. Тесты в `tests/` должны реально выполняться командой хука проверки.

В объёме:
- корневые файлы сборки: `Minizon.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `tests/.editorconfig`;
- проекты `Minizon.Catalog.Domain`, `.Application`, `.Infrastructure`, `.Api` и ссылки между ними;
- `Product` + тип результата (`Result`, `Result<T>`, `DomainError`) по ADR 0002;
- Api на Minimal API, только `GET /health`;
- тестовые проекты: unit-тесты домена, тесты архитектуры (направление зависимостей), тест `/health` в памяти.

Вне объёма (не делать, даже «заодно»): БД, EF Core, gRPC, proto, Docker, Gateway, другие сервисы, фронтенд, CI, Kafka, Redis,
OpenTelemetry, аутентификация, HTTPS-настройки Api. Контракты (`contracts/`) не меняются, поэтому ADR на контракты не нужен.
Хук `.claude/hooks/verify.sh` и CLAUDE.md в варианте C не меняются.

## 2. Принятые решения (кратко; обоснование и варианты — в ADR)

| Вопрос | Решение | Где обосновано |
|---|---|---|
| Тестовый фреймворк | xUnit v3 с отключённым MTP (`xunit.v3.mtp-off`) в режиме VSTest, хук без изменений (вариант C, проверен) | ADR 0001 |
| Ошибки домена | собственный `Result<T>` + `DomainError` с кодами, без исключений для ввода | ADR 0002 |
| Формат решения | `Minizon.slnx` в корне | ADR 0003, п. 1 |
| Имена проектов | `Minizon.<Сервис>.<Слой>` | ADR 0003, п. 2 |
| Строгость сборки | `TreatWarningsAsErrors` + `AnalysisLevel=latest-recommended` | ADR 0003, п. 3 |
| Версии пакетов | центрально, Directory.Packages.props | ADR 0003, п. 4 |
| SDK | global.json, 10.0.302, `latestPatch` | ADR 0003, п. 5 |
| Проверка слоёв | тест, читающий csproj (без внешних библиотек) | ADR 0003, п. 6 |
| CA1707 в тестах | отключить только в tests/ через `tests/.editorconfig` | ADR 0003, п. 7 (открытый вопрос) |

### 2.1. Выбор раннера тестов: варианты и проверка

Известная проблема: на SDK 10.0.302 `dotnet test` для xUnit v3 в режиме Microsoft Testing Platform (MTP) сообщает «ноль тестов»
(код 5). Команда хука (verify.sh, строка 63): `dotnet test --no-build --nologo --filter "Category!=Integration"`.
Синтаксис `--filter "Category!=Integration"` — это синтаксис VSTest; в режиме MTP у xUnit v3 свои параметры (`--filter-not-trait` и др.).

| Вариант | Что меняется | Плюсы | Минусы |
|---|---|---|---|
| A. xUnit v2 + VSTest (рекомендуется) | ничего в хуке и CLAUDE.md | команда хука как есть; привычные флаги; проторённый путь с Testcontainers и WebApplicationFactory | v2 в сопровождении; ноль отобранных тестов, вероятно, даёт код 0 (хук не заметит) |
| B. xUnit v3 + MTP | global.json `test.runner`; хук строка 63 → `dotnet test --no-build --filter-not-trait "Category=Integration"`; CLAUDE.md и test-writer.md — новый синтаксис фильтра | актуальная ветка; ноль тестов, вероятно, даёт ненулевой код | правка хука и инструкций; любой незнакомый аргумент → «ноль тестов» без ясной причины; проект только с Integration-тестами уронит хук |
| C. xUnit v3 в режиме VSTest (MTP выключен) | только пакеты (`xunit.v3.mtp-off`, `xunit.runner.visualstudio` 3.x, `Microsoft.NET.Test.Sdk`) | v3 при неизменном хуке | конфигурация не проверена; тихий ноль тестов, как в A |

Выбран C (решение владельца после Шага 0; результаты — в ADR 0001, «Результаты проверки»). Хук, CLAUDE.md и test-writer.md не меняются.
Раздел 2.2 остаётся справкой на случай будущего перехода на MTP.

### 2.2. Что меняется, если выбран B (справочно, в этой задаче не делать)

- `global.json`: добавить `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Пакеты тестов: `xunit.v3` вместо `xunit`; без `xunit.runner.visualstudio` и `Microsoft.NET.Test.Sdk` (если V2 не покажет, что они нужны);
  в тестовых csproj `<OutputType>Exe</OutputType>`.
- `.claude/hooks/verify.sh`, строка 63: `dotnet test --no-build --filter-not-trait "Category=Integration"` (без `--nologo`).
  Если V2 покажет, что проект без быстрых тестов даёт код 8, добавить `--ignore-exit-code 8` или правило «в каждом тестовом проекте есть быстрые тесты» — решает владелец.
- CLAUDE.md: раздел «Команды», «Быстрые тесты» → `dotnet test --filter-not-trait "Category=Integration"`; в «Правилах работы»
  абзац про интеграционные тесты — тот же синтаксис; добавить предупреждение «не передавать `dotnet test` флаги, которых не знает MTP (например, `--nologo`)».
- `.claude/agents/test-writer.md`, строка 14: синтаксис фильтра.
- ADR 0001: статус варианта B — «принят», A — «отклонён по результатам проверки».

## 3. Затронутые файлы

Новые (developer, если не указано иное):

| Файл | Назначение |
|---|---|
| `Minizon.slnx` | решение; папки решения `/src/Services/Catalog/` и `/tests/` |
| `global.json` | SDK 10.0.302, `rollForward: latestPatch`, `allowPrerelease: false` |
| `Directory.Build.props` | `net10.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `WarningsNotAsErrors` NU1901–NU1904 (если принят открытый вопрос О3 и подтверждена K8) |
| `Directory.Packages.props` | `ManagePackageVersionsCentrally=true`; версии: `xunit.v3.mtp-off` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.12.0, `Microsoft.AspNetCore.Mvc.Testing` 10.0.15 |
| `tests/.editorconfig` | `[*.cs] dotnet_diagnostic.CA1707.severity = none` (если принят О2) |
| `src/Services/Catalog/Minizon.Catalog.Domain/Minizon.Catalog.Domain.csproj` | `Microsoft.NET.Sdk`, без ссылок |
| `src/Services/Catalog/Minizon.Catalog.Domain/Common/DomainError.cs` | запись `DomainError(string Code, string Message)` |
| `src/Services/Catalog/Minizon.Catalog.Domain/Common/Result.cs` | статический класс `Result` с фабриками |
| `src/Services/Catalog/Minizon.Catalog.Domain/Common/ResultOfT.cs` | `Result<T>` |
| `src/Services/Catalog/Minizon.Catalog.Domain/Products/Product.cs` | сущность |
| `src/Services/Catalog/Minizon.Catalog.Domain/Products/ProductErrors.cs` | коды и фабрики ошибок |
| `src/Services/Catalog/Minizon.Catalog.Application/Minizon.Catalog.Application.csproj` | ссылка на Domain; кода нет |
| `src/Services/Catalog/Minizon.Catalog.Infrastructure/Minizon.Catalog.Infrastructure.csproj` | ссылка на Application; кода нет |
| `src/Services/Catalog/Minizon.Catalog.Api/Minizon.Catalog.Api.csproj` | `Microsoft.NET.Sdk.Web`; ссылки на Application и Infrastructure |
| `src/Services/Catalog/Minizon.Catalog.Api/Program.cs` | Minimal API, `/health` |
| `src/Services/Catalog/Minizon.Catalog.Api/appsettings.json`, `appsettings.Development.json` | логирование по умолчанию, без секретов |
| `src/Services/Catalog/Minizon.Catalog.Api/Properties/launchSettings.json` | профиль `http`, `http://localhost:5101` |
| `tests/Minizon.Catalog.UnitTests/Minizon.Catalog.UnitTests.csproj` | ссылка на Domain |
| `tests/Minizon.Catalog.UnitTests/Products/ProductTests.cs` | unit-тесты `Product` (developer), граничные (test-writer) |
| `tests/Minizon.Catalog.UnitTests/Common/ResultTests.cs` | unit-тесты `Result` (developer) |
| `tests/Minizon.ArchitectureTests/Minizon.ArchitectureTests.csproj` | ссылка на `Minizon.Catalog.Domain` (для проверки ссылок сборки) |
| `tests/Minizon.ArchitectureTests/LayerDependencyTests.cs` | тест направления зависимостей |
| `tests/Minizon.ArchitectureTests/RepositoryLayout.cs` | поиск корня репозитория и csproj сервисов |
| `tests/Minizon.Catalog.ApiTests/Minizon.Catalog.ApiTests.csproj` | ссылка на Api; пакет `Microsoft.AspNetCore.Mvc.Testing` (test-writer) |
| `tests/Minizon.Catalog.ApiTests/HealthEndpointTests.cs` | тест `/health` в памяти (test-writer) |

Изменяемые:

| Файл | Изменение | Кто |
|---|---|---|
| `.gitignore` | добавить `.vs/`, `*.user` | developer |
| `docs/adr/0001-…md` | заполнить таблицу «Результаты проверки» | основная сессия, Шаг 0 |
| `docs/agent-log.md` | запись в конце задачи | основная сессия |

Не трогать: `contracts/`, `deploy/`, `src/Web/`, `src/Gateway/`, `.claude/` (в варианте C), `CLAUDE.md` (в варианте C), пустую папку `minizon/`.

## 4. Требования к коду (для developer)

### 4.1. Directory.Build.props и пакеты
- Свойства — по таблице выше. Никаких `NoWarn`, `PackageReference`, `<Nullable>disable</Nullable>` ни здесь, ни в csproj.
- Каждый тестовый csproj: `<OutputType>Exe</OutputType>`, `IsPackable=false`, `IsTestProject=true`; пакеты `xunit.v3.mtp-off`,
  `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` с `PrivateAssets=all`; `<Using Include="Xunit" />`. Пакет `xunit.v3` (с MTP)
  не использовать, секцию `test` в global.json не добавлять (ADR 0001, вариант C).
- Версии пакетов — из таблицы раздела 3 (проверены на Шаге 0). Если новое правило анализатора срабатывает и его нельзя исправить в коде —
  не подавлять, остановиться и описать в отчёте (правило, файл, текст).

### 4.2. Domain (ADR 0002)
- `DomainError` — `public sealed record DomainError(string Code, string Message)`.
- `Result<T> where T : notnull` — `IsSuccess`, `IsFailure`, `Value` (при неуспехе `InvalidOperationException`), `Errors`
  (`IReadOnlyList<DomainError>`, при успехе пустой). Открытого конструктора нет (internal/private).
- `public static class Result` — `Success<T>(T value)`, `Failure<T>(IReadOnlyList<DomainError> errors)`; `null` → `ArgumentNullException`,
  пустой список → `ArgumentException`. Статических членов в `Result<T>` нет (CA1000). Результата без значения не делать.
- `Product`:
  - свойства только для чтения: `Guid Id`, `string Name`, `decimal Price`; закрытый конструктор;
  - `public const int MaxNameLength = 200;`
  - `public static Result<Product> Create(string? name, decimal price)`;
  - имя обрезается по краям (`Trim()`), все проверки — по обрезанному имени, сохраняется обрезанное;
  - `null`, пустая строка или только пробельные символы → `product.name.empty`;
  - длина обрезанного имени (в UTF-16 code units, `string.Length`) больше `MaxNameLength` → `product.name.too_long`;
  - `price <= 0` → `product.price.not_positive`;
  - все нарушения возвращаются сразу (имя и цена вместе → две ошибки), код не повторяется;
  - `Id = Guid.CreateVersion7()` при успешном создании.
- `ProductErrors` — `public static class` с константами кодов (`NameEmptyCode` и т. п.) и методами/свойствами, создающими `DomainError`
  с английским сообщением (сообщение для `too_long` содержит предел).
- Domain не ссылается ни на что: ни `ProjectReference`, ни `PackageReference`, ни `FrameworkReference`.

### 4.3. Application, Infrastructure
Только csproj и ссылки (Application → Domain, Infrastructure → Application). Кода и классов-заглушек нет.

### 4.4. Api
- `Program.cs` на top-level statements: `builder.Services.AddHealthChecks();`, `app.MapHealthChecks("/health");`. Других эндпоинтов,
  Swagger/OpenAPI, HTTPS-перенаправления, аутентификации нет.
- Ожидаемый ответ: `GET /health` → 200, `text/plain`, тело `Healthy`.
- Для WebApplicationFactory нужен доступный `Program`. На Шаге 0 в песочнице `public partial class Program;` не понадобился
  (.NET 10 делает `Program` доступным сам); не добавлять, пока тестовый проект компилируется без него.

### 4.5. Тест архитектуры (ADR 0003, п. 6)
- `RepositoryLayout` поднимается от `AppContext.BaseDirectory` до каталога с `Minizon.slnx`; не нашёл — понятное исключение.
- Перебирает `src/Services/*/**/*.csproj`, разбирает XML (`System.Xml.Linq`), без внешних пакетов.
- Тесты (каждый — отдельный `[Fact]`, имена — смысловые):
  1. найден хотя бы один проект сервиса;
  2. каждый проект назван `Minizon.<Сервис>.<Слой>`, `<Сервис>` совпадает с папкой, `<Слой>` из {Domain, Application, Infrastructure, Api};
  3. прямые `ProjectReference` каждого проекта входят в разрешённый набор: Domain — ∅; Application — {Domain}; Infrastructure — {Application};
     Api — {Application, Infrastructure}; всё в пределах своего сервиса. Сообщение об ошибке называет проект и лишнюю ссылку;
  4. у Domain нет `PackageReference` и `FrameworkReference`, атрибут `Sdk` = `Microsoft.NET.Sdk`;
  5. `typeof(Product).Assembly.GetReferencedAssemblies()` — только имена `System`, `System.*`, `netstandard`, `mscorlib`.
- Разрешённые наборы — одна таблица в коде теста; её изменение — архитектурное решение (через ADR).

## 5. Шаги

### Шаг 0. Проверка гипотез раннера (основная сессия, до запуска developer) — выполнен 2026-11-16

Результаты — в ADR 0001, «Результаты проверки»; все гипотезы Г1–Г6 подтверждены, выбран вариант C. Ниже — процедура для повторения.

Цель: подтвердить или опровергнуть гипотезы ADR 0001 и выбрать вариант. Делается во временной папке вне репозитория
(например, scratchpad сессии), чтобы на опыт не влияли файлы репозитория. Команды — для bash (Git Bash, как в хуке);
в PowerShell вместо `echo $?` использовать `echo $LASTEXITCODE`.
Результаты (код выхода, число тестов, ключевая строка вывода) занести в таблицу «Результаты проверки» ADR 0001.

**V0. Версии пакетов.**
`dotnet --version` → `10.0.302`.
`dotnet package search xunit --exact-match`, то же для `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
`Microsoft.AspNetCore.Mvc.Testing`, `xunit.v3`, `xunit.v3.mtp-off` → записать последние стабильные версии; для
`xunit.runner.visualstudio` — последнюю, поддерживающую xUnit v2 (по описанию пакета на nuget.org).

**V1. Вариант A (гипотезы Г1, Г2).**
Песочница: `global.json` (SDK 10.0.302, `latestPatch`), `Sbx.slnx` (`dotnet new sln -n Sbx --format slnx`), два тестовых проекта
`P1`, `P2` (`dotnet new classlib`, затем в csproj пакеты `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`), оба добавлены в решение.
В `P1`: тест `Fast` (пустой `[Fact]`) и тест `Slow` с `[Trait("Category","Integration")]`, телом `Assert.Fail("integration ran")`.
В `P2`: тест `Fast2` (пустой).
В корне песочницы без указания решения:
1. `dotnet build --nologo -v q; echo $?` → `0`.
2. `dotnet test --no-build --nologo --filter "Category!=Integration"; echo $?` → `0`; в выводе по каждой сборке `Passed: 1`
   (итого выполнено 2), строки `integration ran` нет.
3. `dotnet test --no-build --nologo; echo $?` → `1`; `Failed: 1`, в выводе `integration ran`.
4. Добавить в `P1` тест `Broken` без трейта с `Assert.Fail("fast failed")`, `dotnet build --nologo -v q`, повторить п. 2 → код `1`, в выводе `fast failed`.
   Удалить `Broken`.
5. `dotnet test --no-build --nologo --filter "Category=NoSuch"; echo $?` → записать код (гипотеза Г2: `0` с предупреждением о том,
   что ни один тест не подошёл под фильтр).
Г1 подтверждена, если п. 1–4 дали ожидаемое.

**V2. Вариант B (гипотезы Г3, Г4, Г5).** Отдельная песочница: `global.json` с SDK и `"test": { "runner": "Microsoft.Testing.Platform" }`,
один проект с пакетом `xunit.v3` и `<OutputType>Exe</OutputType>`, тесты `Fast` и `Slow` как в V1. После `dotnet build --nologo -v q`:
1. `dotnet test --no-build --nologo; echo $?` → по Г3: «ноль тестов», код `5`; записать точный текст (есть ли упоминание неизвестного параметра).
2. `dotnet test --no-build; echo $?` → выполнено 2, код ненулевой (падает `Slow`).
3. `dotnet test --no-build --filter-not-trait "Category=Integration"; echo $?` → выполнен 1, код `0` (Г4).
4. `dotnet test --no-build --filter "Category!=Integration"; echo $?` → записать результат (работает ли VSTest-синтаксис).
5. `dotnet test --no-build --filter-trait "Category=NoSuch"; echo $?` → записать код (Г5: ожидается `8` или иной ненулевой).
6. Прямой запуск `./bin/Debug/net10.0/<Проект>.exe` → тесты выполняются (контроль, что проблема не в самом проекте).

**V3. Вариант C (гипотеза Г6).** Песочница без `test.runner` в global.json:
1. Проект с `xunit.v3` (MTP по умолчанию) + `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` 3.x: команда из V1 п. 2 → записать
   результат (по Г6 — ошибка про «VSTest target is no longer supported»).
2. Проект с `xunit.v3.mtp-off` (если пакет существует по V0) + `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` 3.x:
   повторить V1 п. 1–4 → ожидаемое то же, что в V1.

**Правило выбора.** Г1 подтверждена → вариант A, план выполняется как написан. Иначе: остановиться, доложить владельцу результаты V1–V3;
владелец выбирает C или B; при B основная сессия вносит правки из 2.2, а developer получает этот план с пометкой выбранного варианта.

### Шаг 1. Каркас (developer)
1. Создать `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `tests/.editorconfig` по разделам 3–4.1; дополнить `.gitignore`.
2. Создать четыре проекта Catalog и ссылки между ними; создать `Minizon.slnx` и добавить проекты (`dotnet sln Minizon.slnx add …`).
3. `dotnet build --nologo -v q` в корне → код 0.

### Шаг 2. Domain и unit-тесты (developer)
1. Реализовать `DomainError`, `Result`, `Result<T>`, `Product`, `ProductErrors` по 4.2.
2. Создать `tests/Minizon.Catalog.UnitTests`, добавить в решение. Минимальный набор тестов:
   - `ResultTests`: `Success` — `IsSuccess`, `Value`, пустые `Errors`; `Failure` — `IsFailure`, ошибки сохранены; `Value` у неуспеха →
     `InvalidOperationException`; `Failure` с пустым списком → `ArgumentException`; с `null` → `ArgumentNullException`.
   - `ProductTests`: корректные данные → успех, `Name`, `Price` сохранены, `Id != Guid.Empty`; имя с пробелами по краям сохраняется обрезанным;
     `null`, `""`, `"   "` → одна ошибка `product.name.empty` (`[Theory]`); имя длиной 201 → `product.name.too_long`; цена `0` и `-1` →
     `product.price.not_positive`; пустое имя и цена `0` → ровно две ошибки с двумя разными кодами.
   - Проверять коды ошибок, а не тексты сообщений.

### Шаг 3. Api (developer)
1. `Program.cs` по 4.4, `appsettings*.json`, `launchSettings.json`.
2. `dotnet build` → код 0.

### Шаг 4. Тест архитектуры (developer)
1. Создать `tests/Minizon.ArchitectureTests` по 4.5, добавить в решение.
2. Временные негативные проверки K7 (см. раздел 7) выполнить и откатить.

### Шаг 5. Самопроверка developer
Выполнить K1–K7 и K10 из раздела 7. В отчёте: список файлов, вывод итоговых строк `dotnet test` (число тестов по каждой сборке),
сработавшие правила анализаторов и что с ними сделано, отклонения от плана.

### Шаг 6. Дополнительные тесты (test-writer)
Производственный код не менять; найденные ошибки — нумерованным списком (файл, сценарий, ожидаемое, фактическое).
1. `ProductTests` — дополнить, не дублируя тесты developer:
   - граница длины: 1 символ, 199, ровно 200 → успех; 201 → `too_long`;
   - 200 символов плюс пробелы по краям → успех (предел — после обрезки); 201 символ после обрезки → `too_long`;
   - пробельные символы разных видов: `"\t"`, `"\n"`, `"\r\n"`, неразрывный пробел `" "` → `empty`;
   - очень длинное имя (10 000 символов) → ровно одна ошибка `too_long`;
   - суррогатные пары: 100 символов-эмодзи (длина 200 UTF-16) → успех, 101 → `too_long` (фиксирует принятое правило подсчёта длины);
   - цена: наименьшая положительная `0.0000000000000000000000000001m` → успех; `decimal.MaxValue` → успех; `-0.0000000000000000000000000001m`,
     `decimal.MinValue`, `-0m` → `not_positive`;
   - все три вида нарушения: каждый код встречается не больше одного раза; при успехе `Errors` пуст;
   - два успешных `Create` дают разные `Id`.
2. `ResultTests` — пробелы, которых нет у developer (например, `Errors` нельзя изменить через приведение к `List<T>`/массиву, если реализация это допускает — тогда это баг в список).
3. `tests/Minizon.Catalog.ApiTests` (новый проект, добавить в решение и в Directory.Packages.props пакет `Microsoft.AspNetCore.Mvc.Testing`):
   - `GET /health` → 200, `text/plain`, тело `Healthy`;
   - `GET /` и `GET /products` → 404 (эндпоинтов, кроме `/health`, нет).
   Тест в памяти без Docker, поэтому трейта `Integration` не получает (ADR 0001, «Последствия»).
4. Конкурентность и идемпотентность в этой задаче неприменимы: сущность неизменяемая, общего состояния, хранилища и повторяемых
   запросов нет. Отдельных тестов не писать, в отчёте это отметить.
5. Выполнить K3–K5 и K9 (раздел 7).

### Шаг 7. Исправления (developer) — по списку test-writer, в той же ветке. Затем коммит, ревью, PR — по CLAUDE.md.

## 6. Вопросы к владельцу и ответы

Ответы (2026-11-16): О1 — вариант C (решение владельца). О2–О5 — приняты рекомендованные варианты, владелец может изменить до начала Шага 1.

- О1. Раннер: вариант A (рекомендуется) с запасными C/B по результатам Шага 0 — ок?
- О2. CA1707: отключить только в `tests/.editorconfig` (рекомендуется) или писать имена тестов без подчёркиваний?
- О3. NU1901–NU1904 как предупреждения (`WarningsNotAsErrors`, рекомендуется) или как ошибки?
- О4. Предел длины имени 200 символов UTF-16 и обрезка пробелов по краям — ок?
- О5. Цена — `decimal` без валюты и без ограничения числа знаков после запятой (валюта и масштаб — в задаче с заказами/оплатой) — ок?

## 7. Критерии готовности и способ проверки

Все команды — в корне репозитория. «Временная правка» — вносится для проверки и сразу откатывается; в коммит не попадает
(`git status` после отката не показывает изменений в этом файле).

| № | Критерий | Как проверить | Ожидаемо |
|---|---|---|---|
| K1 | Сборка без предупреждений | `dotnet build --no-incremental -clp:Summary; echo $?` | код 0, `0 Warning(s)`, `0 Error(s)` |
| K2 | `.slnx` подхватывается без пути (требует проверки) | `ls *.sln *.slnx *.csproj` → только `Minizon.slnx`; `dotnet sln list` | 6 проектов после шагов developer (4 src + UnitTests + ArchitectureTests), 7 после test-writer; `dotnet build` без пути собирает все (в выводе есть каждый проект) |
| K3 | Команда хука реально выполняет тесты | `dotnet build --nologo -v q && dotnet test --no-build --nologo --filter "Category!=Integration"; echo $?` | код 0; по каждой тестовой сборке строка вида `Passed! - Failed: 0, Passed: N` с N > 0; сумма N = число тестов в проекте |
| K4 | Падающий тест ломает хук | временная правка: `[Fact] public void Sentinel() => Assert.Fail("sentinel");` в `ProductTests`; команда K3; затем сам хук: `rm -f .claude/.last-verify; echo '{}' \| CLAUDE_PROJECT_DIR="$PWD" bash .claude/hooks/verify.sh; echo $?` | команда K3 — код 1, в выводе `sentinel`; хук — код 2, в stderr `dotnet test failed`. После отката хук — код 0 |
| K5 | Integration-тесты исключаются фильтром | временная правка: тест с `[Trait("Category","Integration")]` и `Assert.Fail("integration ran")`; команда K3; затем `dotnet test --no-build --nologo` | K3 — код 0, `integration ran` нет; без фильтра — код 1, `integration ran` есть |
| K6 | Анализаторы активны (требует проверки CA1000/CA1716/CA1707) | временные правки по одной: (а) в `Product` метод `public static void Bad_Name() {}`; (б) в `Result<T>` `public static Result<T> Ok(T value) => …`; (в) переименовать `DomainError` в `Error`; после каждой — `dotnet build --nologo -v q` | (а) ошибка CA1707, (б) CA1000, (в) CA1716; записать в отчёт, какие правила реально сработали. Если ни одно не сработало — анализаторы не включены, это дефект каркаса |
| K7 | Тест слоёв ловит нарушения (требует проверки) | временные правки по одной: (а) в Api `ProjectReference` на Domain; (б) в Domain `PackageReference` на любой пакет из Directory.Packages.props; (в) в Infrastructure ссылка на Domain; после каждой — команда K3 | каждый раз код ≠ 0, падает тест из `Minizon.ArchitectureTests`, сообщение называет проект и лишнюю ссылку; после отката — зелёный |
| K8 | NU190x — предупреждение, а не ошибка (требует проверки; только если принят О3) | в песочнице вне репозитория: копия `Directory.Build.props` + проект с `Newtonsoft.Json` 12.0.1 (известная уязвимость высокой серьёзности), `dotnet build; echo $?`; затем без `WarningsNotAsErrors` | с исключением: предупреждение NU1903, код 0; без — ошибка NU1903, код 1. Нужен доступ к nuget.org. Если с исключением тоже ошибка — исключение убрать, записать в ADR 0003 |
| K9 | `/health` работает, других эндпоинтов нет | тесты `Minizon.Catalog.ApiTests` в K3; вручную: `dotnet run --project src/Services/Catalog/Minizon.Catalog.Api --launch-profile http`, в другом окне `curl -i http://localhost:5101/health` и `curl -i http://localhost:5101/` | 200 `Healthy`; 404. Также проверить, нужен ли был `public partial class Program;` (записать в отчёт) |
| K10 | Domain без зависимостей | `dotnet list src/Services/Catalog/Minizon.Catalog.Domain package` и `dotnet list src/Services/Catalog/Minizon.Catalog.Domain reference`; тесты `Minizon.ArchitectureTests` зелёные | пакетов и ссылок на проекты нет |
| K11 | Объём не превышен, контракты не тронуты | `git status --porcelain` и `git diff --name-only main` | нет изменений в `contracts/`, `deploy/`, `src/Web/`, `src/Gateway/`, `.claude/`, `CLAUDE.md` (в варианте C) |
| K12 | Гипотезы раннера проверены | таблица «Результаты проверки» в ADR 0001 заполнена | каждая строка Г1–Г6 имеет результат и отметку |

Число тестов для K3 (ориентир, не точное): UnitTests — не меньше 15 после developer и больше после test-writer; ArchitectureTests — 5;
ApiTests — 3. Главное — N > 0 в каждой сборке.

## 8. Риски

| Риск | Последствие | Что делаем |
|---|---|---|
| Гипотезы ADR 0001 не подтвердятся | вариант A не работает на 10.0.302 | Шаг 0 до начала кода; запасные C/B; выбор за владельцем |
| В VSTest ноль отобранных тестов даёт код 0 (Г2) | хук «зелёный» без тестов | K3 (N > 0) в критериях каждой задачи; доработку хука владелец может заказать отдельно |
| `latest-recommended` включает правила, которые трудно соблюсти в тестах (CA1515, CA2007, правила xUnit) | сборка красная | исправлять в коде; если нельзя — не подавлять, а остановиться и описать (решение — через ADR 0003) |
| Хук не реагирует на изменения только в `.slnx`, `global.json`, `.editorconfig` | правка этих файлов не проверяется автоматически | в этой задаче рядом всегда меняются `.cs`/`.csproj`; доработка хука — предложение владельцу (ADR 0003) |
| `rollForward: latestPatch` требует SDK 10.0.3xx | на машине без этой полосы сборка не стартует | осознанно: воспроизводимость важнее; CI (позже) ставит ту же полосу |
| `Program` недоступен для WebApplicationFactory | ApiTests не компилируются | 4.4: `public partial class Program;` |
| Тест слоёв ищет корень по `Minizon.slnx` | переименование решения ломает тест | имя фиксировано ADR 0003 |
