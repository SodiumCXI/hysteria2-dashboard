**[Русский](/README.md)** | **[English](/docs/README.en.md)** | **[中文](/docs/README.zh.md)**

# Hysteria2 Dashboard

Web management panel for **[Hysteria2](https://hysteria.network/)** - a fast proxy protocol with censorship bypass capabilities.

![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-20232A?style=flat&logo=dotnet&logoColor=512BD4) ![nginx](https://img.shields.io/badge/nginx-20232A?style=flat&logo=nginx&logoColor=009639) ![Docker](https://img.shields.io/badge/Docker-20232A?style=flat&logo=docker&logoColor=2496ED)

## Features

- Install Hysteria2 and all required dependencies with a single command
- Create and delete users along with their access keys
- Monitor traffic and Hysteria2 service status in real time
- Manage Hysteria2 configuration through the panel interface

## Installation

```bash
curl -fsSL https://raw.githubusercontent.com/SodiumCXI/hysteria2-dashboard/main/install.sh | bash
```

### What gets installed

- **Hysteria2** - binary, systemd service, self-signed TLS certificate, config file
- **Docker** - if not already installed
- **sudo** - if not already installed
- **python3-bcrypt** - if not already installed (used for hashing the administrator password)
- **Dashboard** - containers deployed to `/opt/hysteria2-dashboard/`
- **UFW rules** - ports are opened for Hysteria2 (UDP) and the panel (TCP)

After installation, the panel URL will be displayed in the format `https://<IP>:<PORT>/<SALT>/` along with the administrator password you entered.

## API

All endpoints except `/api/auth/login` require `Authorization: Bearer <token>`.

Every API request automatically includes the `X-Route-Salt` header - the frontend reads the salt from the URL and attaches it to each request. The backend validates it and returns `444` if the check fails.

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/auth/login` | Login, returns a JWT token |
| `GET` | `/api/users` | List all users |
| `POST` | `/api/users` | Create a user |
| `DELETE` | `/api/users/{username}` | Delete a user |
| `GET` | `/api/settings` | Get Hysteria2 configuration |
| `PUT` | `/api/settings` | Save Hysteria2 configuration |
| `POST` | `/api/hysteria/restart` | Restart the Hysteria2 service |
| `SignalR` | `/hubs/traffic` | Traffic statistics, `ReceiveTraffic` event once per second |
| `SignalR` | `/hubs/status` | Service status, `ReceiveStatus` event once per second |

## Uninstallation

Run the same command again - the script will detect the installed panel and offer a complete removal.