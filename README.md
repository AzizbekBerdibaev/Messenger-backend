# Messenger Backend API

Современный бэкенд для мессенджера реального времени, построенный на базе **.NET 8** с использованием **Clean Architecture** и паттерна **CQRS**.

## 🚀 Технологический стек

- **Платформа:** .NET 8 (ASP.NET Core Web API)
- **Архитектура:** Clean Architecture, CQRS (через [MediatR](https://github.com/jbogard/MediatR))
- **База данных:** PostgreSQL (через Entity Framework Core)
- **Кэширование & OTP:** Redis (через StackExchange.Redis)
- **Real-time взаимодействие:** SignalR (WebSockets)
- **Валидация:** FluentValidation
- **Логирование:** Serilog
- **Документация API:** Swagger / OpenAPI

## ✨ Основной функционал

1. **Авторизация (Passwordless):** Вход по номеру телефона через одноразовые пароли (OTP) и JWT-токены (Access + Refresh).
2. **Профиль и Адресная книга:** Синхронизация контактов, установка аватарок, статуса (Bio) и глобальный поиск пользователей.
3. **Чаты и Сообщения:**
   - Приватные и групповые чаты.
   - Поддержка текстовых, голосовых (с передачей длительности) и медиа-сообщений (с подписями).
   - Индикация статусов доставки сообщений (Sent, Delivered, Read).
   - Индикаторы набора текста ("печатает...") в реальном времени.
4. **Истории (Stories):** Публикация фото/видео историй, которые автоматически исчезают через 24 часа.
5. **Продвинутые функции:**
   - Интеграция с S3 (выдача Pre-signed ссылок для загрузки медиа).
   - Push-уведомления (регистрация FCM Device Tokens).
   - Сигнальный сервер для аудио/видео звонков WebRTC (CallOffer, CallAnswer, ICE Candidates).
   - Управление черным списком (Block / Unblock).

## 📂 Структура проекта

Проект разделен на 4 классических слоя Clean Architecture:

- `Messenger.Domain` — Ядро. Сущности (Entities), Enum-ы и интерфейсы репозиториев/сервисов.
- `Messenger.Application` — Бизнес-логика. CQRS обработчики (Commands/Queries), DTO и валидаторы. Никаких зависимостей от инфраструктуры.
- `Messenger.Infrastructure` — Реализация интерфейсов. EF Core (ApplicationDbContext), SignalR (ChatHub), сервисы Redis, генерация JWT токенов.
- `Messenger.WebApi` — Точка входа. ASP.NET Core контроллеры, настройка Middleware, Swagger, DI контейнер и `Dockerfile`.

## 🛠 Запуск проекта локально

### Требования
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Запущенные инстансы **PostgreSQL** и **Redis**.

### Шаги

1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/ВАШ_ЛОГИН/НАЗВАНИЕ_РЕПОЗИТОРИЯ.git
   cd Messenger
   ```

2. **Настройте переменные окружения:**
   Откройте `src/Messenger.WebApi/appsettings.Development.json` и укажите свои строки подключения к PostgreSQL и Redis, а также секретный ключ JWT:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=messenger;Username=postgres;Password=your_password"
     },
     "Redis": {
       "ConnectionString": "localhost:6379"
     },
     "JwtSettings": {
       "Secret": "ReplaceWithAVeryLongSecretKeyMin32Chars!",
       "Issuer": "MessengerApi",
       "Audience": "MessengerClients",
       "ExpirationMinutes": 60,
       "RefreshExpirationDays": 30
     }
   }
   ```

3. **Примените миграции базы данных:**
   В корне проекта выполните:
   ```bash
   dotnet ef database update --project src/Messenger.Infrastructure --startup-project src/Messenger.WebApi
   ```

4. **Запустите проект:**
   ```bash
   dotnet run --project src/Messenger.WebApi
   ```

После запуска API документация (Swagger) будет доступна по адресу: `http://localhost:5000/swagger`

## 🐳 Запуск через Docker

Проект содержит готовый `Dockerfile`. Для деплоя на платформы вроде Render, Railway или собственный VPS:

```bash
# Сборка образа
docker build -t messenger-api .

# Запуск контейнера (не забудьте передать переменные окружения)
docker run -p 8080:8080 messenger-api
```

## 🧪 Тестирование

Проект покрыт юнит-тестами, тестами архитектуры и интеграционными E2E тестами с использованием **Testcontainers**.

```bash
# Запуск всех тестов
dotnet test
```

## 📱 Интеграция с мобильным приложением (Flutter)

Для команды мобильной разработки подготовлено специальное руководство:
👉 [**Руководство по интеграции бэкенда для Flutter-разработчиков (API Guide)**](flutter_api_guide.md)

Здесь подробно описаны форматы ответов, авторизация через Telegram-бота, работа с WebSocket (SignalR) и отправка файлов.
