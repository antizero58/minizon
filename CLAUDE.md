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

## Архитектура
- Сервисы: Gateway (YARP, выдача и проверка JWT), Catalog, Cart, Ordering, Inventory, Notifications.
- У каждого сервиса своя БД. Чужую БД читать запрещено — только gRPC или события.
- Синхронно — gRPC (контракты в contracts/proto), асинхронно — Kafka (схемы в contracts/events).
- Слои сервиса: Domain → Application → Infrastructure → Api. Domain ни от чего не зависит.
- Фронтенд: src/Web (SPA). Ходит только в Gateway по REST, напрямую к сервисам — никогда.
- В проде SPA и API на одном домене (ingress: / → Web, /api → Gateway), поэтому CORS не нужен.
- Аутентификация: Gateway выдаёт JWT в HttpOnly-куке (Secure, SameSite=Strict); токен не хранится в localStorage.
- Структура:
  - src/Services/<Name>/ — сервисы
  - src/Gateway/ — шлюз
  - src/Web/ — фронтенд
  - contracts/proto, contracts/events — контракты
  - deploy/ — docker-compose, Helm-чарты
  - docs/adr/ — архитектурные решения, docs/specs/ — спецификации
  - tests/ — тесты бэкенда

## Правила работы
- Одна задача — одна ветка и один PR. Маленькие шаги.
- Сначала план, код — только после моего «ок».
- Контракты (proto, события, OpenAPI Gateway) меняются только через ADR.
- Тесты к каждому изменению: unit для домена, интеграционные через Testcontainers, для фронтенда — Vitest.
- Не отключать проверки (nullable, предупреждения, проверку сертификатов) ради «чтобы собралось».
- Секреты не коммитить; конфигурация через appsettings + переменные окружения.
- Архитектурные решения — в docs/adr/NNNN-название.md.

## Команды
- Сборка: dotnet build
- Тесты: dotnet test
- Окружение: docker compose -f deploy/docker-compose.yml up -d
- Фронтенд: cd src/Web && npm install && npm run dev   (тесты: npm test, сборка: npm run build)

## Обо мне
Я backend-разработчик на .NET. Код пишут агенты, я ставлю задачи, утверждаю планы и ревьюю.
Объясняй неочевидные решения кратко; если есть варианты — предлагай 2–3 с плюсами и минусами.