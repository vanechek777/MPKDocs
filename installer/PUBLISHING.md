# Публикация новых версий МПК.Документы

## Способ 1 — через админ-панель (рекомендуется)

1. Соберите установщик на ПК:
   ```powershell
   .\installer\build-installer.ps1
   ```
2. Увеличьте версию в `MPKDocumentsMAUI.csproj`:
   ```xml
   <ApplicationDisplayVersion>1.0.1</ApplicationDisplayVersion>
   <ApplicationVersion>2</ApplicationVersion>
   ```
3. Задеплойте **API** (нужны маршруты `/admin/app-release/publish` и раздача `/releases/…`).
4. В приложении: **Админ-панель** → **Публикация версии клиента**.
5. Нажмите **Следующая версия** (подставит version/build автоматически).
6. Выберите платформу (Windows / Android).
7. Выберите файл `.exe` (или `.apk`).
8. Нажмите **Загрузить и опубликовать**.

API сохранит файл в `/data/releases` на Amvera и пропишет ссылку в `app_release`.  
Клиенты увидят обновление при следующем запуске.

### Переменная Amvera (опционально)

Если ссылки на файлы должны вести на основной домен, а не на tuna:

```
MPK_PUBLIC_BASE_URL=https://mpk-docs.ru
```

---

## Способ 2 — полный скрипт с ПК

Одна команда: сборка + загрузка на сервер:

```powershell
$env:MPK_API_BASE_URL = "https://mpk-docs.ru.tuna.am"
$env:MPK_ADMIN_PHONE = "+79148012594"
$env:MPK_ADMIN_PASSWORD = "ваш_пароль"

.\installer\publish-release.ps1 -Notes "Исправления и улучшения"
```

Версия и build берутся из `.csproj`. Можно переопределить:

```powershell
.\installer\publish-release.ps1 -Version "1.0.2" -Build 3 -Mandatory
```

---

## Поля версии

| Поле | Описание |
|------|----------|
| `ApplicationDisplayVersion` | Строка для пользователя (`1.0.1`) |
| `ApplicationVersion` | **build** — целое число, всегда увеличивайте |
| `mandatory` | Пользователь не может отложить обновление |
| `min_build` | Все с build ниже — обязательное обновление |

---

## Проверка

```bash
curl https://ваш-api/config/app-release
curl -I https://ваш-api/releases/MPKDocuments-1.0.1-b2-windows.exe
```

В приложении: **Профиль** → **Проверить обновления**.

---

## Android / iOS

- **Android:** соберите APK, в админке выберите платформу Android и загрузите файл.
- **iOS:** IPA через админку или внешняя ссылка (App Store / TestFlight).

Сборка мобильных пакетов на сервере Amvera **не выполняется** — только хостинг готовых файлов.
