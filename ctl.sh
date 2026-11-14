#!/bin/bash

set -euo pipefail

[ "$EUID" -ne 0 ] && { echo "Error: run as root" >&2; exit 1; }

export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"

DASHBOARD_DIR="/opt/hysteria2-dashboard"
COMPOSE_FILE="${DASHBOARD_DIR}/docker-compose.yml"
COMPOSE_ENV="${DASHBOARD_DIR}/.env"
CONFIG_FILE="/etc/hysteria/config.yaml"

die() { echo "$*" >&2; exit 1; }

valid_port() { [[ "$1" =~ ^[1-9][0-9]{0,4}$ ]] && [ "$1" -le 65535 ]; }

fw_switch() {
  local proto="$1" old="$2" new="$3"
  [ "$old" != "$new" ] || return 0
  command -v ufw &>/dev/null || return 0
  ufw allow "${new}/${proto}" >/dev/null 2>&1 || true
  ufw delete allow "${old}/${proto}" >/dev/null 2>&1 || true
}

cmd_restart() {
  systemctl restart hysteria-server || true
  sleep 2

  if systemctl is-active --quiet hysteria-server; then
    rm -f "${CONFIG_FILE}.bak"
    return 0
  fi

  if [ -f "${CONFIG_FILE}.bak" ]; then
    cat "${CONFIG_FILE}.bak" > "$CONFIG_FILE"
    rm -f "${CONFIG_FILE}.bak"
    systemctl restart hysteria-server || true
    die "Hysteria2 failed to start, previous config restored"
  fi

  die "Hysteria2 failed to start. Check: journalctl -u hysteria-server -n 50"
}

cmd_firewall() {
  local old="$1" new="$2"
  valid_port "$old" || die "Invalid port"
  valid_port "$new" || die "Invalid port"
  fw_switch udp "$old" "$new"
}

cmd_port() {
  grep -Po '^DASH_PORT=\K[0-9]+$' "$COMPOSE_ENV"
}

cmd_dashboard() {
  local port="$1" salt="$2" old_port
  valid_port "$port" || die "Invalid port"
  [[ "$salt" =~ ^[A-Za-z0-9]{1,64}$ ]] || die "Invalid route salt"

  old_port=$(cmd_port)
  cp -p "$COMPOSE_ENV" "${COMPOSE_ENV}.bak"
  sed -i -e "s/^DASH_PORT=.*/DASH_PORT=${port}/" -e "s/^ROUTE_SALT=.*/ROUTE_SALT=${salt}/" "$COMPOSE_ENV"

  if ! docker compose -f "$COMPOSE_FILE" up -d >/dev/null 2>&1; then
    mv -f "${COMPOSE_ENV}.bak" "$COMPOSE_ENV"
    docker compose -f "$COMPOSE_FILE" up -d >/dev/null 2>&1 || true
    die "Dashboard failed to start, previous settings restored"
  fi

  rm -f "${COMPOSE_ENV}.bak"
  fw_switch tcp "$old_port" "$port"
}

case "${1:-}" in
  restart)   cmd_restart ;;
  firewall)  cmd_firewall "${2:-}" "${3:-}" ;;
  port)      cmd_port ;;
  dashboard) cmd_dashboard "${2:-}" "${3:-}" ;;
  *)         die "Unknown command" ;;
esac
