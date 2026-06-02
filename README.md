**[Русский](/README.md)** | **[English](/docs/README.en.md)** | **[中文](/docs/README.zh.md)**

# Hysteria2 Dashboard
 
Веб-панель управления для **[Hysteria2](https://hysteria.network/)** - быстрого прокси-протокола с обходом блокировок.
 
![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-20232A?style=flat&logo=dotnet&logoColor=512BD4) ![nginx](https://img.shields.io/badge/nginx-20232A?style=flat&logo=nginx&logoColor=009639) ![Docker](https://img.shields.io/badge/Docker-20232A?style=flat&logo=docker&logoColor=2496ED)
 
## Возможности
 
- Установка Hysteria2 и всего необходимого окружения одной командой
- Создание и удаление пользователей вместе с их ключами доступа
- Мониторинг трафика и статуса службы Hysteria2 в реальном времени
- Управление конфигурацией Hysteria2 через интерфейс панели

## Установка
 
```bash
curl -fsSL https://raw.githubusercontent.com/SodiumCXI/hysteria2-dashboard/main/install.sh | bash
```

### Что будет установлено
 
- **Hysteria2** - бинарник, systemd-сервис, самоподписанный TLS-сертификат, конфиг
- **Docker** - если не установлен
- **sudo** - если не установлен
- **python3-bcrypt** - если не установлен (для хеширования пароля администратора)
- **Dashboard** - контейнеры в `/opt/hysteria2-dashboard/`
- **Правила UFW** - открываются порты для Hysteria2 (UDP) и панели (TCP)

После установки будет выведен URL панели вида `https://<IP>:<PORT>/<SALT>/` и введённый вами пароль администратора.

## API
 
Все эндпоинты, кроме `/api/auth/login`, требуют `Authorization: Bearer <token>`.

Каждый запрос к API автоматически содержит заголовок `X-Route-Salt` - фронтенд берёт соль из URL и подставляет её при каждом запросе. Бэкенд проверяет её корректность и возвращает `444` если проверка не прошла.
 
| Тип | Путь | Описание |
|-----|------|----------|
| `POST` | `/api/auth/login` | Вход, возвращает JWT токен |
| `GET` | `/api/users` | Список пользователей |
| `POST` | `/api/users` | Создать пользователя |
| `DELETE` | `/api/users/{username}` | Удалить пользователя |
| `GET` | `/api/settings` | Получить конфигурацию Hysteria2 |
| `PUT` | `/api/settings` | Сохранить конфигурацию Hysteria2 |
| `POST` | `/api/hysteria/restart` | Перезапустить службу Hysteria2 |
| `SignalR` | `/hubs/traffic` | Статистика трафика, событие `ReceiveTraffic` раз в секунду |
| `SignalR` | `/hubs/status` | Статус службы, событие `ReceiveStatus` раз в секунду |
 
## Удаление
 
Запустите ту же команду повторно - скрипт обнаружит установленную панель и предложит полное удаление.
