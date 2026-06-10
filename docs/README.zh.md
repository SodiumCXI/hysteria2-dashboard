**[Русский](/README.md)** | **[English](/docs/README.en.md)** | **[中文](/docs/README.zh.md)**

# Hysteria2 控制面板

**[Hysteria2](https://hysteria.network/)** 的网页管理面板 - 一款支持翻墙的高速代理协议。

![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-20232A?style=flat&logo=dotnet&logoColor=512BD4) ![nginx](https://img.shields.io/badge/nginx-20232A?style=flat&logo=nginx&logoColor=009639) ![Docker](https://img.shields.io/badge/Docker-20232A?style=flat&logo=docker&logoColor=2496ED)

> 适合需要快速部署 Hysteria2、无需手动配置各组件的用户的开箱即用方案。

## 功能

- 一条命令完成 Hysteria2 及所有必要环境的安装
- 创建和删除用户及其访问密钥
- 实时监控流量及 Hysteria2 服务状态
- 通过面板界面管理 Hysteria2 配置
- 自动从 Let's Encrypt 为 IP 地址申请 TLS 证书

## 安装

```bash
curl -fsSL https://raw.githubusercontent.com/SodiumCXI/hysteria2-dashboard/main/install.sh | bash
```

### 安装内容

- **Hysteria2** - 二进制文件、systemd 服务、配置文件
- **控制面板** - 容器部署至 `/opt/hysteria2-dashboard/`
- **Docker** - 运行面板容器
- **acme.sh** - 为 IP 申请并自动续期 Let's Encrypt 证书
- **sudo** - 通过 ssh 以 root 权限执行命令
- **python3-bcrypt** - 管理员密码哈希处理

安装完成后，将显示格式为 `https://<IP>:<PORT>/<SALT>/` 的面板访问地址及您所设置的管理员密码。

## 兼容性

本面板与其他使用 Let's Encrypt IP 证书的解决方案兼容（例如 **3x-ui**），可以在不冲突的情况下叠加安装。*（如果您使用的是域名证书而非 IP 证书，则不保证兼容性，大概率会出现问题。）*

## API

除 `/api/auth/login` 外，所有接口均需携带 `Authorization: Bearer <token>` 请求头。

URL 中的 salt 值会自动附加到每个请求的 `X-Route-Salt` 请求头中。后端会对其进行验证，不匹配时返回 `444`。

| 方法 | 路径 | 说明 |
|------|------|------|
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