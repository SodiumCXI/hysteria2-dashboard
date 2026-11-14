**[Русский](/README.md)** | **[English](/docs/README.en.md)** | **[中文](/docs/README.zh.md)**

# Hysteria2 Dashboard

Web management panel for **[Hysteria2](https://hysteria.network/)** - a fast proxy protocol with censorship bypass capabilities.

![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-20232A?style=flat&logo=dotnet&logoColor=512BD4) ![nginx](https://img.shields.io/badge/nginx-20232A?style=flat&logo=nginx&logoColor=009639) ![Docker](https://img.shields.io/badge/Docker-20232A?style=flat&logo=docker&logoColor=2496ED)

> A ready-to-use solution for those who need Hysteria2 up and running without manually configuring each component.

## Features

- Install Hysteria2 and all required dependencies with a single command
- Create and delete users along with their access keys
- Monitor traffic, Hysteria2 service status and server load in real time
- Per-user traffic accounting in a database - statistics survive restarts
- Manage Hysteria2 configuration and the panel port, address and password through the interface
- Automatic TLS certificate provisioning from Let's Encrypt for IP addresses

## Installation

```bash
curl -fsSL https://raw.githubusercontent.com/SodiumCXI/hysteria2-dashboard/main/install.sh | bash
```

### What gets installed

- **Hysteria2** - binary, systemd service, config file
- **Dashboard** - containers deployed to `/opt/hysteria2-dashboard/`
- **Docker** - running the panel containers
- **acme.sh** - provisioning and auto-renewal of Let's Encrypt certificate for IP
- **sudo** - executing commands as root via ssh
- **python3-bcrypt** - hashing the administrator password

After installation, the panel URL will be displayed in the format `https://<IP>:<PORT>/<SALT>/` along with the administrator password you entered.

## Keys

By default the client verifies the server's Let's Encrypt certificate. If your client fails to connect, set an SNI in the panel settings: keys will then be issued with `sni` and `insecure=1`, without certificate verification.

## Compatibility

The panel is compatible with other solutions that use Let's Encrypt certificates for IP (e.g. **3x-ui**) and can be installed alongside them without conflicts. *(If you are using domain certificates rather than IP certificates, compatibility is not guaranteed and things will likely break.)*

## API

All endpoints except `/api/auth/login` require `Authorization: Bearer <token>`.

The salt from the URL is automatically attached to the `X-Route-Salt` header of each request. The backend validates it and returns `444` on mismatch.

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/auth/login` | Login, returns a JWT token |
| `GET` | `/api/users` | List all users |
| `POST` | `/api/users` | Create a user |
| `DELETE` | `/api/users/{username}` | Delete a user |
| `GET` | `/api/settings` | Get Hysteria2 and panel settings |
| `PUT` | `/api/settings` | Save Hysteria2 and key settings |
| `PUT` | `/api/settings/password` | Change the administrator password, returns a new JWT token |
| `POST` | `/api/restart/hysteria` | Restart the Hysteria2 service |
| `POST` | `/api/restart/dashboard` | Apply the panel port and salt, restart nginx |
| `SignalR` | `/hubs/traffic` | Accumulated per-user traffic, `ReceiveTraffic` event once per second |
| `SignalR` | `/hubs/status` | Service status, `ReceiveStatus` event once per second |
| `SignalR` | `/hubs/usage` | CPU and RAM usage, `ReceiveUsage` event once per second |

For SignalR the salt is passed in the `salt` parameter of the connection URL.

## Updating and uninstallation

Run the same command again - the script will detect the installed panel and offer to update or completely remove it.