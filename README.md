# Windows ISO Downloader

Консольная утилита (.NET Framework 4.8 / C#) для автоматического получения официальной прямой ссылки на загрузку оригинального ISO-образа Windows 11 с серверов Microsoft.

## Как это работает

1. Инициализирует сессию через официальный Microsoft Software Download API (подход Fido).
2. Запрашивает прямую ссылку на последнюю версию **Windows 11 (Russian, x64)**.
3. Копирует полученную ссылку прямо в буфер обмена Windows.

## Требования

- Windows 10 / 11
- .NET Framework 4.8 Runtime

## Сборка

Проект использует современный SDK-style `.csproj`:

```powershell
dotnet build -c Release
```

Исполняемый файл будет собран в `bin/x64/Release/net48/GetWindowsIso.exe`.
