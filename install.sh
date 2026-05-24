#!/bin/bash

set -euo pipefail

[ "$EUID" -ne 0 ] && { echo "Error: run as root"; exit 1; }

GREEN='\033[0;32m'
NC='\033[0m'

GITHUB_REPO="SodiumCXI/hysteria2-dashboard"

CONFIG_FILE="/etc/hysteria/config.yaml"
APP_JSON="/etc/hysteria/app.json"
DASHBOARD_DIR="/opt/hysteria2-dashboard"
COMPOSE_ENV="${DASHBOARD_DIR}/.env"

rand()  { tr -dc 'A-Za-z0-9' </dev/urandom | head -c "$1"; true; }
rand_b64() { tr -dc 'A-Za-z0-9+/' </dev/urandom | head -c "$1"; true; }
die()   { echo "Error: $*"; exit 1; }

tcp_port_free() {
  ! ss -tln | grep -q ":${1} "
}

udp_port_free() {
    ! ss -uln | grep -q ":${1} "
}

cmd_install() {
  exec </dev/tty

  echo "Downloading Dashboard files..."
  VERSION=$(curl -fsSL "https://api.github.com/repos/${GITHUB_REPO}/releases/latest" \
    | grep -Po '"tag_name": "\K[^"]+')
  [ -z "$VERSION" ] && die "Could not fetch latest release version"

  mkdir -p "$DASHBOARD_DIR"
  curl -fsSL "https://github.com/${GITHUB_REPO}/releases/download/${VERSION}/release.tar.gz" \
    | tar -xz -C "$DASHBOARD_DIR"
  echo "Done."

  SERVER_IP=$(curl -s --max-time 5 https://api.ipify.org || curl -s --max-time 5 https://ifconfig.me)
  [ -z "$SERVER_IP" ] && die "Could not detect external IP"

  echo ""
  echo "Hysteria2 + Dashboard Installer"
  echo "Leave blank to keep defaults"
  echo ""

  while true; do
    read -rp "Hysteria2 port [443]: " _in
    H2_PORT="${_in:-443}"
    if udp_port_free "$H2_PORT"; then
      break
    else
      echo "UDP port $H2_PORT is already in use."
    fi
  done
  read -rp "SNI [google.com]: " _in;       SNI="${_in:-google.com}"
  read -rp "Key name [Hysteria2]: " _in;   KEY_NAME="${_in:-Hysteria2}"
  read -rp "First username [User]: " _in;  FIRST_USER="${_in:-User}"
  while true; do
    read -rp "Dashboard port [443]: " _in
    DASH_PORT="${_in:-443}"
    if tcp_port_free "$DASH_PORT"; then
      break
    else
      echo "TCP port $DASH_PORT is already in use."
    fi
  done
  while true; do
    read -rsp "Dashboard admin password: " ADMIN_PASS
    echo
    if [ -z "$ADMIN_PASS" ]; then
      echo "Password cannot be empty."
    else
      break
    fi
  done

  echo ""

  OBFS_PASS=$(rand 32)
  FIRST_PASS=$(rand 32)
  JWT_SECRET=$(rand_b64 64)
  TRAFFIC_SECRET=$(rand 32)
  ROUTE_SALT=$(rand 16)

  export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"

  echo "Installing Hysteria2..."

  set +o pipefail

  script -qefc 'bash <(curl -fsSL https://get.hy2.sh/)' /dev/null |
  python3 -u -c '
import sys

needle = b"Congratulation"
tail = b""
output_enabled = True

while True:
    ch = sys.stdin.buffer.read(1)
    if not ch:
        break

    if output_enabled:
        sys.stdout.buffer.write(ch)
        sys.stdout.buffer.flush()

        tail = (tail + ch)[-len(needle):]
        if needle in tail:
            output_enabled = False
'

  stty sane 2>/dev/null || true

  set -o pipefail

  echo "Generating certificate..."
  mkdir -p /etc/hysteria
  chown hysteria:hysteria /etc/hysteria
  chmod 750 /etc/hysteria

  openssl req -x509 -nodes -newkey rsa:2048 \
    -keyout /etc/hysteria/server.key \
    -out /etc/hysteria/server.crt \
    -days 36500 -subj "/CN=${SERVER_IP}" 2>/dev/null

  chown hysteria:hysteria /etc/hysteria/server.key /etc/hysteria/server.crt
  chmod 600 /etc/hysteria/server.key
  chmod 644 /etc/hysteria/server.crt
  echo "Done."

  echo "Writing Hysteria2 config..."
  cat > "$CONFIG_FILE" <<CONF
listen: :${H2_PORT}
tls:
  cert: /etc/hysteria/server.crt
  key: /etc/hysteria/server.key
bandwidth:
  up: 1 gbps
  down: 1 gbps
auth:
  type: userpass
  userpass:
    ${FIRST_USER}: ${FIRST_PASS}
masquerade:
  type: proxy
  proxy:
    url: https://${SNI}/
    rewriteHost: true
obfs:
  type: salamander
  salamander:
    password: ${OBFS_PASS}
trafficStats:
  listen: :9999
  secret: ${TRAFFIC_SECRET}
CONF

  chown hysteria:hysteria "$CONFIG_FILE"
  chmod 600 "$CONFIG_FILE"
  echo "Done."

  echo "Configuring firewall..."
  if command -v ufw &>/dev/null; then
    ufw allow "${H2_PORT}/udp" >/dev/null 2>&1
    ufw allow "${DASH_PORT}/tcp" >/dev/null 2>&1
    echo "Done."
  else
    echo "Warning: ufw not found. Open ports ${H2_PORT}/udp and ${DASH_PORT}/tcp manually."
  fi

  echo "Setting up SSH access for Dashboard..."
  if ! command -v sudo &>/dev/null; then
    apt install -y -qq sudo
  fi

  SSH_PORT=$(grep -Po '^Port \K\d+' /etc/ssh/sshd_config 2>/dev/null || echo "22")

  mkdir -p /etc/sudoers.d
  echo "hysteria ALL=(ALL) NOPASSWD: /bin/systemctl" > /etc/sudoers.d/hysteria
  chmod 440 /etc/sudoers.d/hysteria

  ssh-keygen -t ed25519 -f "${DASHBOARD_DIR}/ssh_key" -N ""
  mkdir -p /var/lib/hysteria/.ssh
  cat "${DASHBOARD_DIR}/ssh_key.pub" >> /var/lib/hysteria/.ssh/authorized_keys
  chown -R hysteria:hysteria /var/lib/hysteria/.ssh
  chmod 700 /var/lib/hysteria/.ssh
  chmod 600 /var/lib/hysteria/.ssh/authorized_keys
  chmod 600 "${DASHBOARD_DIR}/ssh_key"

  echo "Done."

  echo "Starting Hysteria2..."
  systemctl enable hysteria-server >/dev/null 2>&1
  systemctl restart hysteria-server
  sleep 1
  systemctl is-active --quiet hysteria-server \
    || die "Hysteria2 failed. Check: journalctl -u hysteria-server -n 50"
  echo "Done."

  if ! command -v docker &>/dev/null; then
    echo "Installing Docker..."
	
    set +o pipefail

    script -qefc 'curl -fsSL https://get.docker.com | sh' /dev/null |
    python3 -u -c '
import sys

needle = b"INFO"
tail = b""
output_enabled = True

while True:
    ch = sys.stdin.buffer.read(1)
    if not ch:
        break

    if output_enabled:
        sys.stdout.buffer.write(ch)
        sys.stdout.buffer.flush()

        tail = (tail + ch)[-len(needle):]
        if needle in tail:
            output_enabled = False
'

    stty sane 2>/dev/null || true

    set -o pipefail
	
    systemctl enable docker >/dev/null 2>&1
    systemctl start docker
    echo "Done."
  else
    echo "Docker already installed, skipping."
  fi

  echo "Hashing admin password..."

  if ! python3 -c "import bcrypt" 2>/dev/null; then
    apt install -y -qq python3-bcrypt
  fi

  ADMIN_HASH=$(python3 -c 'import bcrypt, sys; print(bcrypt.hashpw(sys.argv[1].encode(), bcrypt.gensalt()).decode())' "$ADMIN_PASS")

  echo "Done."

  echo "Writing app.json..."
  cat > "$APP_JSON" <<JSON
{
  "jwtSecret": "${JWT_SECRET}",
  "trafficApiSecret": "${TRAFFIC_SECRET}",
  "adminPasswordHash": "${ADMIN_HASH}",
  "serverIP": "${SERVER_IP}",
  "keyName": "${KEY_NAME}",
  "routeSalt": "${ROUTE_SALT}"
}
JSON

  chmod 600 "$APP_JSON"
  echo "Done."

  echo "Setting up Dashboard..."

  cat > "$COMPOSE_ENV" <<ENV
ROUTE_SALT=${ROUTE_SALT}
DASH_PORT=${DASH_PORT}
SSH_PORT=${SSH_PORT}
GITHUB_REPO=${GITHUB_REPO}
VERSION=${VERSION}
ENV

  chmod 600 "$COMPOSE_ENV"

  echo "Done."

  echo "Starting Dashboard..."
  docker compose -f "${DASHBOARD_DIR}/docker-compose.yml" pull
  docker compose -f "${DASHBOARD_DIR}/docker-compose.yml" up -d
  echo "Done."

  echo ""
  printf "${GREEN}Installation complete!${NC}\n"
  echo ""
  echo "Dashboard:"
  echo "  URL: https://${SERVER_IP}:${DASH_PORT}/${ROUTE_SALT}"
  echo "  Password: $ADMIN_PASS"
  echo ""
}

cmd_uninstall() {
  exec </dev/tty

  read -rp "Remove Hysteria2, Dashboard and all data? [y/N]: " _in
  [ "${_in,,}" != "y" ] && { echo "Aborted."; exit 0; }

  local h2_port="" dash_port=""
  if [ -f /etc/hysteria/config.yaml ]; then
    h2_port=$(grep -Po '^listen: :\K\d+' /etc/hysteria/config.yaml || true)
  fi
  if [ -f "${COMPOSE_ENV}" ]; then
    dash_port=$(grep -Po '^DASH_PORT=\K\d+' "${COMPOSE_ENV}" || true)
  fi

  echo "Stopping Hysteria2 service..."
  systemctl stop hysteria-server 2>/dev/null || true
  systemctl disable hysteria-server 2>/dev/null || true
  echo "Done."

  echo "Removing Hysteria2 binary..."
  bash <(curl -fsSL https://get.hy2.sh/) --remove >/dev/null 2>&1 || true
  echo "Done."

  echo "Removing systemd units..."
  rm -f /etc/systemd/system/multi-user.target.wants/hysteria-server.service
  rm -f /etc/systemd/system/multi-user.target.wants/hysteria-server@*.service
  rm -f /etc/systemd/system/hysteria-server.service
  rm -f /etc/systemd/system/hysteria-server@.service
  rm -f /lib/systemd/system/hysteria-server.service
  rm -f /lib/systemd/system/hysteria-server@.service
  systemctl daemon-reload
  echo "Done."

  echo "Removing hysteria user..."
  if id "hysteria" &>/dev/null; then
    userdel -r hysteria 2>/dev/null || userdel hysteria 2>/dev/null || true
	rm -f /etc/sudoers.d/hysteria
    echo "Done."
  else
    echo "User not found, skipping."
  fi

  echo "Removing Dashboard containers and files..."
  if [ -f "${DASHBOARD_DIR}/docker-compose.yml" ]; then
    docker compose -f "${DASHBOARD_DIR}/docker-compose.yml" down -v --rmi all 2>/dev/null || true
  fi
  
  docker image prune -f 2>/dev/null || true
  rm -rf "${DASHBOARD_DIR}"
  echo "Done."

  echo "Removing config and certificates..."
  rm -rf /etc/hysteria
  echo "Done."

  echo "Removing firewall rules..."
  if command -v ufw &>/dev/null; then
    [ -n "$h2_port" ]   && { ufw delete allow "${h2_port}/udp"   >/dev/null 2>&1 || true; }
    [ -n "$dash_port" ] && { ufw delete allow "${dash_port}/tcp" >/dev/null 2>&1 || true; }
    echo "Done."
  else
    echo "Warning: ufw not found. Remove firewall rules for ports ${h2_port}/udp and ${dash_port}/tcp manually."
  fi

  echo "Removing Docker..."
  read -rp "Remove Docker as well? [y/N]: " _rm_docker
  if [ "${_rm_docker,,}" = "y" ]; then
    systemctl stop docker 2>/dev/null || true
    systemctl disable docker 2>/dev/null || true
    apt-get remove -y -qq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin 2>/dev/null || true
    apt-get autoremove -y -qq 2>/dev/null || true
    rm -rf /var/lib/docker /etc/docker
    echo "Done."
  else
    echo "Skipped."
  fi

  echo ""
  printf "${GREEN}Hysteria2 and Dashboard removed.${NC}\n"
  echo ""
}

if [ -f "${APP_JSON}" ]; then
  echo "Dashboard is already installed."
  cmd_uninstall
  exit 0
else
  cmd_install
  exit 0
fi