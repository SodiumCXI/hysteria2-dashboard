**[Русский](/README.md)** | **[English](/docs/README.en.md)** | **[中文](/docs/README.zh.md)**

# Hysteria2 Dashboard

Веб-панель управления для **[Hysteria2](https://hysteria.network/)** - быстрого прокси-протокола с обходом блокировок.

![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-20232A?style=flat&logo=dotnet&logoColor=512BD4) ![nginx](https://img.shields.io/badge/nginx-20232A?style=flat&logo=nginx&logoColor=009639) ![Docker](https://img.shields.io/badge/Docker-20232A?style=flat&logo=docker&logoColor=2496ED)

> Готовое решение для тех, кому нужно быстро поднять Hysteria2 без ручной настройки каждого компонента.

<p align="center">
  <img src="/docs/Dashboard-ru.png" alt="Dashboard screenshot">
</p>

## Возможности

- Установка Hysteria2 и всего необходимого окружения одной командой
- Создание и удаление пользователей вместе с их ключами доступа
- Мониторинг трафика и статуса службы Hysteria2 в реальном времени
- Управление конфигурацией Hysteria2 через интерфейс панели
- Автоматическое получение TLS-сертификатов от Let's Encrypt для IP-адреса

## Установка

```bash
curl -fsSL https://raw.githubusercontent.com/SodiumCXI/hysteria2-dashboard/main/install.sh | bash
```

### Что будет установлено
 
- **Hysteria2** - бинарник, systemd-сервис, конфиг
- **Dashboard** - контейнеры в `/opt/hysteria2-dashboard/`
- **Docker** - запуск контейнеров панели
- **acme.sh** - получение и автообновление сертификата Let's Encrypt для IP
- **sudo** - выполнение команд от root для ssh
- **python3-bcrypt** - хеширование пароля администратора

После установки будет выведен URL панели вида `https://<IP>:<PORT>/<SALT>/` и введённый вами пароль администратора.

## Совместимость
 
Панель совместима с другими решениями, использующими сертификаты Let's Encrypt для IP (например, **3x-ui**) - её можно установить поверх без конфликтов. *(Если вы используете сертификаты для домена, а не IP, совместимость не гарантируется и скорее всего что-то сломается.)*


## API

Все эндпоинты, кроме `/api/auth/login`, требуют `Authorization: Bearer <token>`.

Соль из URL автоматически подставляется в заголовок `X-Route-Salt` каждого запроса. Бэкенд проверяет её и возвращает `444` при несовпадении.

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
