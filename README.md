# ETLModule | Конвейер данных

**ETLModule** — это универсальное кроссплатформенное десктопное приложение для извлечения, трансформации и загрузки данных (ETL). Проект разработан с строгим соблюдением принципов **SOLID** и **Чистой Архитектуры**, что позволяет легко масштабировать поддерживаемые форматы файлов и системы управления базами данных.

![Avalonia UI](https://img.shields.io/badge/UI-Avalonia-purple.svg?style=flat-square)
![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg?style=flat-square)
![Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen.svg?style=flat-square)

## Ключевые возможности

* **Мультиплатформенные СУБД:** Полная поддержка работы с **SQLite**, **PostgreSQL** и **MS SQL Server** «из коробки».
* **Умный парсинг файлов:** Импорт и экспорт данных в форматах **CSV, JSON, XML и Excel (.xlsx)**.
* **Автоматический вывод типов:** Встроенный анализатор (`DataTypeAnalyzer`) самостоятельно определяет типы данных (числа, даты, GUID, строки) при чтении файлов и генерирует правильную DDL-схему для создания таблиц в базе данных.
* **Настраиваемые политики импорта:** Разрешение конфликтов первичных ключей (UPSERT):
    * *Обновлять* — замена старых записей новыми.
    * *Игнорировать* — пропуск дубликатов.
    * *Исключение* — строгий контроль и остановка при конфликтах.
* **Современный интерфейс:** Написан на Avalonia UI (MVVM) с поддержкой мгновенного переключения Светлой/Темной темы.

## Архитектура проекта

Проект логически и физически разделен на три независимых слоя, что гарантирует низкую связность (Loose Coupling) и высокую тестируемость кода:

1.  **`ETLModule.Core` (Ядро)**
    Сердце конвейера. Не имеет никаких зависимостей от пользовательского интерфейса. Содержит всю бизнес-логику:
    * **Извлечение (Extract):** `IFileImporter` и `FileHandlerFactory`.
    * **Трансформация (Transform):** `IDataTypeAnalyzer` и `IDataTransformer`.
    * **Загрузка (Load):** `IDynamicRepository` с использованием паттерна *Стратегия* (`ISqlDialect`) и *Абстрактная Фабрика* (`IRepositoryFactory`, `IDbConnectionFactory`).
2.  **`ETLModule.Desktop` (Пользовательский интерфейс)**
    Тонкий клиент на базе паттерна **MVVM**. Отвечает исключительно за привязку данных (Data Binding) и взаимодействие с пользователем. Зависимости внедряются через конструкторы (Dependency Injection).
3.  **`ETLModule.ConsoleTest` (Интеграционные тесты)**
    Консольное приложение для сквозного тестирования всего ETL-цикла в изолированной среде.

## Быстрый старт

### Требования
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

### Установка и запуск

1. Склонируйте репозиторий:
   ```bash
   git clone [https://github.com/your-username/ETLModule.git](https://github.com/your-username/ETLModule.git)
   cd ETLModule
   ```
   
2. **Настройка конфигурации (ВАЖНО):**
В папке проекта `ETLModule.Desktop` найдите файл `appsettings.example.json`. Скопируйте его, переименуйте в `appsettings.json` и укажите актуальные данные для подключения к вашим базам данных:

    ``` json
    {
       "ConnectionStrings": {
       "Sqlite": "Data Source=DATABASE.db",
       "MsSql": "Server=SERVER;Database=DATABASE;Trusted_Connection=True;TrustServerCertificate=True;",
       "Postgres": "Host=HOST;Port=PORT;Database=DATABASE;Username=USERNAME;Password=PASSWORD"
       },
       "CurrentDatabase": "Sqlite"
    }
    ```

    *(Файл `appsettings.json` добавлен в `.gitignore` для защиты ваших учетных данных).*


3. Сборка и запуск приложения:
    ```bash
    dotnet run --project ETLModule.Desktop

    ```



## Паттерны проектирования

В ходе разработки применялись следующие архитектурные подходы:

* **Dependency Inversion (DI):** Инъекция сервисов в `MainWindowViewModel`.
* **Strategy:** Различные диалекты SQL (`PostgresDialect`, `SqliteDialect`) инкапсулируют специфику формирования запросов для разных СУБД.
* **Abstract Factory:** Фабрики репозиториев и обработчиков файлов (`RepositoryFactory`, `FileHandlerFactory`) скрывают сложность инстанцирования конкретных реализаций.

## Лицензия

Проект распространяется под лицензией MIT. Подробности см. в файле [LICENSE](https://www.google.com/search?q=LICENSE).