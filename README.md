# Windows ISO Downloader

Консольная утилита (.NET Framework 4.8 / C#) для автоматического получения официальной прямой ссылки на загрузку оригинального ISO-образа Windows 11 с серверов Microsoft.

## Как это работает

1. Инициализирует сессию через официальный Microsoft Software Download API (подход Fido).
2. Запрашивает прямую ссылку на выбранную редакцию и язык (по умолчанию Windows 11, Russian, x64).
3. Копирует полученную ссылку в буфер обмена Windows.

## Параметры командной строки

```text
GetWindowsIso.exe [опции]
  --lang, -l <name>   Язык (по умолчанию: Russian)
  --locale <code>     Локаль запроса (по умолчанию: ru-ru)
  --edition, -e <id>  ID редакции продукта (по умолчанию: 3262 - Win 11)
  --arch, -a <1|2>    Архитектура (1: x64, 2: x86/arm64, по умолчанию: 1)
  --no-clipboard      Не копировать ссылку в буфер обмена
  --quiet, -q         Выводить только прямую ссылку (для скриптов и CI)
  --help, -h          Справка
```

## Требования

- Windows 10 / 11
- .NET Framework 4.8 Runtime

## Сборка

Проект использует современный SDK-style `.csproj`:

```powershell
dotnet build -c Release
```

Исполняемый файл будет собран в `bin/Release/net48/GetWindowsIso.exe`.
