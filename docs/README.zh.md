**[Русский](/README.md)** | **[English](/docs/README.en.md)** | **[中文](/docs/README.zh.md)**

# Hysteria2 控制面板

**[Hysteria2](https://hysteria.network/)** 的网页管理面板 - 一款支持翻墙的高速代理协议。

![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-20232A?style=flat&logo=dotnet&logoColor=512BD4) ![nginx](https://img.shields.io/badge/nginx-20232A?style=flat&logo=nginx&logoColor=009639) ![Docker](https://img.shields.io/badge/Docker-20232A?style=flat&logo=docker&logoColor=2496ED)

## 功能

- 一条命令完成 Hysteria2 及所有必要环境的安装
- 创建和删除用户及其访问密钥
- 实时监控流量及 Hysteria2 服务状态
- 通过面板界面管理 Hysteria2 配置

## 安装

```bash
curl -fsSL https://raw.githubusercontent.com/SodiumCXI/hysteria2-dashboard/main/install.sh | bash
```

### 安装内容

- **Hysteria2** - 二进制文件、systemd 服务、自签名 TLS 证书、配置文件
- **Docker** - 如未安装则自动安装
- **sudo** - 如未安装则自动安装
- **python3-bcrypt** - 如未安装则自动安装（用于管理员密码的哈希处理）
- **控制面板** - 容器部署至 `/opt/hysteria2-dashboard/`
- **UFW 规则** - 自动开放 Hysteria2（UDP）和面板（TCP）所需端口

安装完成后，将显示格式为 `https://<IP>:<PORT>/<SALT>/` 的面板访问地址及您所设置的管理员密码。

## API

除 `/api/auth/login` 外，所有接口均需携带 `Authorization: Bearer <token>` 请求头。

每个 API 请求会自动包含 `X-Route-Salt` 请求头 - 前端从 URL 中读取 salt 值并在每次请求时附加。后端会验证其正确性，验证失败时返回 `444`。

| 方法 | 路径 | 说明 |
|---|---|---|
| `POST` | `/api/auth/login` | 登录，返回 JWT 令牌 |
| `GET` | `/api/users` | 获取用户列表 |
| `POST` | `/api/users` | 创建用户 |
| `DELETE` | `/api/users/{username}` | 删除用户 |
| `GET` | `/api/settings` | 获取 Hysteria2 配置 |
| `PUT` | `/api/settings` | 保存 Hysteria2 配置 |
| `POST` | `/api/hysteria/restart` | 重启 Hysteria2 服务 |
| `SignalR` | `/hubs/traffic` | 流量统计，每秒推送一次 `ReceiveTraffic` 事件 |
| `SignalR` | `/hubs/status` | 服务状态，每秒推送一次 `ReceiveStatus` 事件 |

## 卸载

再次运行同一命令 - 脚本将检测到已安装的面板并提示完整卸载。