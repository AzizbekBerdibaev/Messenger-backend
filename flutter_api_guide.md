# Руководство по интеграции бэкенда для Flutter-разработчиков 🚀

Добро пожаловать в документацию API для мобильного приложения **Messenger**. 
Здесь подробно описаны все эндпоинты, форматы запросов, логика WebSocket (SignalR) и уникальный процесс авторизации через Telegram Bot.

> [!TIP]
> Все запросы к API (кроме `send-otp` и `verify-otp`) требуют передачи JWT-токена в заголовке:
> `Authorization: Bearer <ваш_access_token>`

## Рекомендуемые библиотеки (pubspec.yaml)
Для комфортной реализации этого API рекомендуются следующие пакеты:
- **`dio`** — для HTTP запросов и настройки Interceptors (для Refresh токена).
- **`signalr_netcore`** — для Real-time чата (WebSocket).
- **`flutter_secure_storage`** — для безопасного хранения JWT-токенов.
- **`url_launcher`** — для перенаправления (Deep Link) в Telegram-бота (`tg://resolve...`).
- **`image_picker`** — для выбора медиа при публикации историй.
- **`hive` или `isar`** — для кэширования чатов и работы в оффлайне.

---

## 1. Базовая настройка и обработка ошибок

### Формат ответов об ошибках (Problem Details)
Бэкенд строго следует стандарту `ProblemDetails` (RFC 7807) для всех ошибок. Ваш `http.Client` или `Dio` в Flutter должен быть готов парсить такой формат при получении статусов `400`, `401`, `404`, `409` или `500`.

**Пример ответа при ошибке валидации (400 Bad Request):**
```json
{
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "PhoneNumber": ["Неверный формат номера телефона. Используйте +998XXXXXXXXX."]
  }
}
```

---

## 2. Авторизация (Telegram OTP Flow) 🔐

Процесс авторизации происходит по номеру телефона (строго в формате **+998XXXXXXXXX**) без паролей. Код подтверждения отправляется пользователю в личные сообщения через Telegram-бота.

### 2.1. Отправка OTP-кода
`POST /api/auth/send-otp`

**Тело запроса:**
```json
{
  "phoneNumber": "+998901234567"
}
```

> [!IMPORTANT]
> **Умная обработка ответов (КРИТИЧНО ДЛЯ FLUTTER)**
> 
> Бэкенд проверяет, привязан ли пользователь к Telegram-боту.
> 
> - **Сценарий А (Успех):** Статус `204 No Content`. Код отправлен в Telegram. Переводите пользователя на экран ввода кода.
> - **Сценарий Б (Нужна привязка):** Статус `409 Conflict`. 
>   ```json
>   {
>     "status": 409,
>     "title": "Conflict",
>     "detail": "User not registered in Telegram bot. Please start the bot and share your contact first."
>   }
>   ```
>   **Ваши действия во Flutter на статус 409:**
>   Покажите модальное окно: *"Для получения кода авторизации перейдите в нашего бота и поделитесь контактом"*.
>   Кнопка в окне должна вызывать `url_launcher` с ссылкой `tg://resolve?domain=YOUR_BOT_USERNAME`.

### 2.2. Проверка OTP-кода (Вход в систему)
`POST /api/auth/verify-otp`

**Тело запроса:**
```json
{
  "phoneNumber": "+998901234567",
  "code": "1234"
}
```

**Ответ (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUz...",
  "refreshToken": "dGVzdF9yZWZyZXNo...",
  "isNewUser": true,
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "phoneNumber": "+998901234567",
    "displayName": null
  }
}
```
*Сохраните токены в Flutter `flutter_secure_storage`.*

### 2.3. Обновление токена (Refresh)
`POST /api/auth/refresh`

**Тело запроса:**
```json
{
  "refreshToken": "ваш_сохраненный_refresh_token"
}
```

---

## 3. Контакты 👥

Пользователи могут добавлять другие номера в свои контакты для создания чатов.

### 3.1. Добавить контакт
`POST /api/contacts`

**Тело запроса:**
```json
{
  "phoneNumber": "+998991234567",
  "firstName": "Азизбек",
  "lastName": "Бердибаев"
}
```
*(Возвращает `204 No Content` при успехе. Если пользователя с таким номером еще нет в системе, он может быть приглашен).*

### 3.2. Получить список контактов
`GET /api/contacts`

**Ответ (200 OK):**
```json
[
  {
    "id": "guid",
    "phoneNumber": "+998991234567",
    "firstName": "Азизбек",
    "lastName": "Бердибаев",
    "userId": "guid_пользователя_если_зарегистрирован",
    "isRegistered": true
  }
]
```

---

## 4. Чаты и Сообщения 💬

### 4.1. Получить список чатов пользователя
`GET /api/chats`

Возвращает список всех личных и групповых чатов с последним сообщением (для главного экрана).

### 4.2. Создать личный чат
`POST /api/chats/personal`

**Тело запроса:**
```json
{
  "targetUserId": "guid_собеседника"
}
```

### 4.3. Получить сообщения чата (Пагинация)
`GET /api/chats/{chatId}/messages?page=1&pageSize=50`

**Ответ (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "text": "Привет!",
      "senderId": "guid",
      "createdAt": "2026-07-10T10:00:00Z"
    }
  ],
  "totalCount": 150
}
```

---

## 5. Работа с WebSocket (SignalR) ⚡

Для получения сообщений в реальном времени (Real-time), Flutter-приложению необходимо использовать пакет `signalr_netcore` (или аналогичный).

**Endpoint Hub'а:** `/hubs/chat`

### Подключение (Flutter пример):
```dart
final hubConnection = HubConnectionBuilder()
    .withUrl(
      'https://your-api.com/hubs/chat',
      options: HttpConnectionOptions(
        accessTokenFactory: () async => await getAccessToken(),
      ),
    )
    .build();

await hubConnection.start();
```

### События для прослушивания (Listen):
- **`ReceiveMessage`** - Срабатывает, когда вам приходит новое сообщение.
  *Аргументы: `(MessageDto message)`*
- **`UserTyping`** - Кто-то печатает в чате.
  *Аргументы: `(string chatId, string userId)`*

### Отправка сообщения через Hub:
Вместо REST API, отправка сообщений обычно делается прямо через активное соединение WebSocket для минимальной задержки:
```dart
hubConnection.invoke('SendMessage', args: [chatId, "Привет, мир!"]);
```

---

## 6. Истории (Stories) 📸

Пользователи могут выкладывать истории, которые удаляются через 24 часа.

> [!WARNING]
> Так как истории содержат медиафайлы (фото/видео), запрос отправляется в формате `multipart/form-data`, а не JSON!

### 6.1. Опубликовать историю
`POST /api/stories`

**Заголовки:** `Content-Type: multipart/form-data`
**Тело (FormData):**
- `mediaFile`: [Файл (File / Blob)]
- `caption`: "Отличный день!" (String, опционально)
- `durationSeconds`: 15 (Integer, опционально)

### 6.2. Лента историй (Get Feed)
`GET /api/stories/feed`

Возвращает сгруппированные по пользователям активные (не истекшие) истории контактов.

**Ответ (200 OK):**
```json
[
  {
    "userId": "guid",
    "userName": "Азизбек",
    "stories": [
      {
        "id": "guid",
        "mediaUrl": "https://s3.amazonaws.com/...",
        "caption": "Отличный день!",
        "createdAt": "2026-07-10T08:00:00Z",
        "expiresAt": "2026-07-11T08:00:00Z"
      }
    ]
  }
]
```

---

## 💡 Советы для архитектуры на Flutter

1. **Interceptor (Dio)**: Напишите интерцептор, который будет ловить статусы `401 Unauthorized` и автоматически пытаться обновить токен через `/api/auth/refresh`, а затем повторять упавший запрос.
2. **Изолированный сервис SignalR**: Держите соединение с Hub'ом в виде отдельного Singleton-сервиса, чтобы при навигации между экранами чатов не переподключаться заново.
3. **Кэширование**: Для списка контактов и историй используйте локальную БД (например, Hive или Isar), чтобы приложение работало даже в оффлайн-режиме, синхронизируя данные при появлении сети.
