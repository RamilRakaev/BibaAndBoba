# MessengerApp

Простой MVP обмена личными текстовыми сообщениями:

- `Messenger.Api` — ASP.NET Core Web API, PostgreSQL, EF Core, cookie-авторизация, SignalR
- `Messenger.Web` — Blazor Server UI

## Сервисы и порты

| Сервис | Локально | Docker Compose |
| --- | --- | --- |
| Messenger.Web | http://localhost:8080 | http://localhost:8080 |
| Messenger.Api | http://localhost:8081 | http://localhost:8081 |
| PostgreSQL | localhost:5432 | localhost:5432 |
| SignalR hub | http://localhost:8081/hubs/chat | внутренний адрес `http://api:8080/hubs/chat` |

## Пример строки подключения PostgreSQL

```text
Host=localhost;Port=5432;Database=messenger_db;Username=messenger_user;Password=messenger_password
```

В Docker API использует:

```text
Host=postgres;Port=5432;Database=messenger_db;Username=messenger_user;Password=messenger_password
```

## Начальный администратор

По умолчанию:

- UserName: `admin`
- Password: `admin`
- DisplayName: `Administrator`

Данные задаются в `src/Messenger.Api/appsettings.json` в секции `AdminSeed`. Пароль в базу сохраняется только как хеш через `PasswordHasher`. Если администратора ещё нет, он создаётся при старте API, в лог пишется сообщение без пароля.

## Как запустить локально

1. Установите .NET 8 SDK и PostgreSQL 16.
2. Создайте базу `messenger_db`, пользователя `messenger_user` и пароль `messenger_password`.
3. Либо поднимите только PostgreSQL через Docker:

```bash
docker compose up postgres -d
```

4. Примените миграции:

```bash
dotnet ef database update --project src/Messenger.Api --startup-project src/Messenger.Api
```

При обычном запуске API миграции также применяются автоматически.

5. Запустите API и Web:

```bash
dotnet run --project src/Messenger.Api
dotnet run --project src/Messenger.Web
```

6. Откройте http://localhost:8080 и войдите как `admin` / `admin`.

## Как запустить через Docker Compose

На Ubuntu VDS из корня репозитория:

```bash
docker compose up --build -d
```

Откройте http://`IP-сервера`:8080 и войдите как `admin` / `admin`.

Остановка:

```bash
docker compose down
```

Данные PostgreSQL хранятся в volume `postgres_data`.

## Как применить EF Core migrations

Создать миграцию:

```bash
dotnet ef migrations add InitialCreate --project src/Messenger.Api --startup-project src/Messenger.Api --output-dir Migrations
```

Применить миграцию:

```bash
dotnet ef database update --project src/Messenger.Api --startup-project src/Messenger.Api
```

При старте `Messenger.Api` вызывает `Database.Migrate()`, поэтому отдельный шаг не обязателен для Docker и обычного запуска.

## Как зайти под начальным админом

1. Откройте веб-приложение: http://localhost:8080
2. Введите `admin` / `admin`
3. После входа откроется список чатов
4. Через «Управление пользователями» создайте обычных пользователей и начните переписку со страницы «Пользователи»

## Где поменять данные администратора

Файл: `src/Messenger.Api/appsettings.json`

```json
"AdminSeed": {
  "UserName": "admin",
  "Password": "admin",
  "DisplayName": "Administrator"
}
```

Seed выполняется только если в базе ещё нет пользователя с ролью `Admin`.
