# Minizon
Учебный маркетплейс (мини-версия Ozon/Amazon): каталог → корзина → заказ → резерв → оплата → уведомление.
Не связан с Ozon и Amazon. Цель — микросервисная архитектура и агентская разработка.

## Стек
- Бэкенд: .NET 10, ASP.NET Core, gRPC, EF Core + PostgreSQL, Redis, Kafka (Outbox/Inbox).
- Наблюдаемость: OpenTelemetry → OTel Collector → Prometheus (метрики) + Jaeger (трейсы) → Grafana.
- Фронтенд: React + TypeScript + Vite, React Router, TanStack Query (запросы к API и кэш),
  формы — React Hook Form + Zod.
- Тесты: xUnit + Testcontainers (бэкенд), Vitest + Testing Library (фронтенд), Playwright (e2e).
- Инфраструктура: Docker Compose (локально), Kubernetes + Helm (k3d локально, k3s на сервере), GitHub Actions.
  Деплой будет позже.

## Архитектура
- Сервисы: Gateway, Catalog, Cart, Ordering, Inventory, Notifications.
- Gateway — BFF на ASP.NET Core Minimal API: снаружи REST, внутри gRPC-клиенты к сервисам. Выдаёт и проверяет JWT.
  Логику сервисов в Gateway не переносим. YARP сейчас не используем: реверс-прокси (YARP) перед Gateway
  добавим позже вместе с деплоем, решается отдельным ADR.
- У каждого сервиса своя БД. Чужую БД читать запрещено — только gRPC или события.
- Redis: Cart — хранилище корзины, Catalog — кэш, Gateway — rate limit.
- Синхронно — gRPC (контракты в contracts/proto), асинхронно — Kafka (схемы в contracts/events).
  REST-контракт Gateway — contracts/openapi/gateway.json.
- Слои сервиса: Domain → Application → Infrastructure → Api. Domain ни от чего не зависит.
- Фронтенд: src/Web (SPA). Ходит только в Gateway по REST, напрямую к сервисам — никогда.
  API-клиент генерируется из contracts/openapi/gateway.json в src/Web/src/api и руками не правится.
- SPA и API на одном origin, CORS не используем ни в dev, ни в проде:
  в dev — прокси Vite `/api` → Gateway; в проде — ingress (`/` → Web, `/api` → Gateway), целевая схема, деплой позже.
- Аутентификация: Gateway выдаёт JWT в HttpOnly-куке (Secure, SameSite=Strict); токен не хранится в localStorage.
  Gateway передаёт JWT сервисам в метаданных gRPC (`authorization`); сервисы проверяют подпись и берут userId из claims.
  Проверка владельца ресурса — в каждом сервисе, на «уже проверил Gateway» не полагаемся.
  Алгоритм подписи и распространение ключа — отдельный ADR.
- Структура:
  - src/Services/<Name>/ — сервисы
  - src/Gateway/ — шлюз
  - src/Web/ — фронтенд (тесты фронтенда рядом с компонентами: *.test.tsx; src/Web/src/api — генерируемый клиент)
  - contracts/proto, contracts/events, contracts/openapi — контракты
  - deploy/ — docker-compose, Helm-чарты
  - docs/adr/ — архитектурные решения, docs/specs/ — спецификации, docs/plans/ — планы задач
  - docs/agent-log.md — журнал работы с агентами
  - tests/ — тесты бэкенда, tests/e2e — Playwright

## Порядок работы
1. Задача.
2. План в docs/plans/NNNN-название.md. Пишет `architect`, если меняются контракты или это новая функциональность;
   иначе план пишет основная сессия.
3. Мой «ок» на план.
4. Основная сессия создаёт ветку; `developer` пишет код и unit-тесты.
5. `test-writer` добавляет интеграционные, граничные и конкурентные тесты; найденные баги возвращает списком.
6. `developer` исправляет баги в той же ветке.
7. Основная сессия делает коммит.
8. `reviewer` ревьюит ветку после коммита, до PR; `developer` вносит исправления.
9. Основная сессия открывает PR через `gh`. Сливаю я.
10. В конце каждой задачи основная сессия предлагает запись для docs/agent-log.md.

Git (ветки, коммиты, PR) делает только основная сессия, не субагенты.
Субагентам при запуске передаётся путь к файлу плана: они не видят этот чат.

## Правила работы
- Одна задача — одна ветка и один PR. Маленькие шаги.
- План обязателен, если меняется больше одного файла, контракт или поведение. Код — только после моего «ок» на план.
  Опечатки и мелкие правки — без плана.
- Контракты (proto, события, contracts/openapi/gateway.json) меняются только через ADR: ADR и сам контракт — вместе.
- Тесты к каждому изменению: unit для домена пишет `developer` вместе с кодом; интеграционные через Testcontainers,
  граничные и конкурентные — `test-writer`; для фронтенда — Vitest; сквозные — Playwright.
- Не отключать проверки (nullable, предупреждения, проверку сертификатов) ради «чтобы собралось».
- Секреты не коммитить; конфигурация через appsettings + переменные окружения.
- Архитектурные решения — в docs/adr/NNNN-название.md.
- Коммиты — Conventional Commits на английском. Имена в коде и комментарии — на английском.
  ADR, планы и спецификации — на русском.

## Команды
- Сборка: dotnet build
- Тесты: dotnet test
- Окружение: docker compose -f deploy/docker-compose.yml up -d
- Фронтенд: cd src/Web && npm install && npm run dev   (тесты: npm test, сборка: npm run build)
- PR: gh pr create

## Обо мне
Я backend-разработчик на .NET. Код пишут агенты, я ставлю задачи, утверждаю планы и ревьюю.
Объясняй неочевидные решения кратко; если есть варианты — предлагай 2–3 с плюсами и минусами.
