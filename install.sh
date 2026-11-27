#!/bin/bash

set -euo pipefail

[ "$EUID" -ne 0 ] && { echo "Error: run as root"; exit 1; }

export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

GITHUB_REPO="sodiumcxi/hysteria2-dashboard"
CONF_DIR="/etc/hysteria"
CONFIG_FILE="${CONF_DIR}/config.yaml"
APP_JSON="${CONF_DIR}/app.json"
STATE_FILE="${CONF_DIR}/install_state"
DASHBOARD_DIR="/opt/hysteria2-dashboard"
COMPOSE_FILE="${DASHBOARD_DIR}/docker-compose.yml"
COMPOSE_ENV="${DASHBOARD_DIR}/.env"
CTL="${DASHBOARD_DIR}/ctl.sh"
CERT_DIR="/root/cert/ip"
SYSCTL_FILE="/etc/sysctl.d/99-hysteria.conf"
DROPIN_DIR="/etc/systemd/system/hysteria-server.service.d"
SUDOERS_FILE="/etc/sudoers.d/hysteria"

rand()     { tr -dc 'A-Za-z0-9' </dev/urandom | head -c "$1"; true; }
rand_b64() { tr -dc 'A-Za-z0-9+/' </dev/urandom | head -c "$1"; true; }
die()      { echo "Error: $*"; exit 1; }

valid_port() { [[ "$1" =~ ^[1-9][0-9]{0,4}$ ]] && [ "$1" -le 65535 ]; }
valid_sni()  { [[ "$1" =~ ^[A-Za-z0-9.-]{0,64}$ ]]; }
valid_user() { [[ "$1" =~ ^[a-z0-9_-]{1,32}$ ]]; }
valid_ipv4() { [[ "$1" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]]; }

tcp_port_free() {
  ! ss -tln | grep -q ":${1} "
}

udp_port_free() {
  ! ss -uln | grep -q ":${1} "
}

print_color() {
  local color="$1"
  local text="$2"
  printf "%b%s%b\n" "$color" "$text" "$NC"
}

state_set() {
  local key="$1"
  local value="$2"
  sed -i "s/^${key}=.*/${key}=${value}/" "$STATE_FILE"
}

state_get() {
  grep -Po "^${1}=\K[01]$" "$STATE_FILE" 2>/dev/null || echo 0
}

install_acme() {
  if command -v ~/.acme.sh/acme.sh &>/dev/null; then
    return 0
  fi
  echo "Installing acme.sh..."
  cd ~ || return 1
  curl -fsSL https://get.acme.sh | sh > /dev/null 2>&1 \
    || die "Failed to install acme.sh"
  echo "Done."
}

_fix_cert_permissions() {
  chmod o+x /root 2>/dev/null || true
  chmod o+x /root/cert 2>/dev/null || true
  chmod o+x /root/cert/ip 2>/dev/null || true
  chown root:hysteria "${CERT_DIR}/privkey.pem" "${CERT_DIR}/fullchain.pem"
  chmod 640 "${CERT_DIR}/privkey.pem"
  chmod 644 "${CERT_DIR}/fullchain.pem"
}

setup_ssl_cert() {
  local ip="$1"

  local our_reload="systemctl reload hysteria-server 2>/dev/null || true; docker compose -f /opt/hysteria2-dashboard/docker-compose.yml restart 2>/dev/null || true"

  if [[ -f "${CERT_DIR}/fullchain.pem" && -f "${CERT_DIR}/privkey.pem" ]]; then
    echo "Found existing certificate in ${CERT_DIR} — reusing it."

    local conf_file=~/.acme.sh/"${ip}_ecc"/"${ip}.conf"
    local existing_reload=""
    if [[ -f "$conf_file" ]]; then
      local encoded
      encoded=$(grep 'Le_ReloadCmd=' "$conf_file" \
        | sed "s/Le_ReloadCmd='__ACME_BASE64__START_\(.*\)__ACME_BASE64__END_'/\1/" || true)
      if [[ -n "$encoded" ]]; then
        existing_reload=$(echo "$encoded" | base64 -d 2>/dev/null || true)
      fi
    fi

    local final_reload="${our_reload}"
    if [[ -n "$existing_reload" ]]; then
      if echo "$existing_reload" | grep -q "hysteria-server"; then
        final_reload="${existing_reload}"
      else
        final_reload="${our_reload}; ${existing_reload}"
      fi
    fi

    ~/.acme.sh/acme.sh --installcert -d "${ip}" \
      --key-file       "${CERT_DIR}/privkey.pem" \
      --fullchain-file "${CERT_DIR}/fullchain.pem" \
      --reloadcmd      "${final_reload}" \
      > /dev/null 2>&1 || true

    _fix_cert_permissions
    CERT_FILE="${CERT_DIR}/fullchain.pem"
    KEY_FILE="${CERT_DIR}/privkey.pem"

    return 0
  fi

  install_acme

  state_set INSTALLED_ACME 1

  echo "Issuing Let's Encrypt IP certificate for ${ip}..."
  echo "Note: port 80 must be reachable from the internet."
  mkdir -p "$CERT_DIR"

  if command -v ufw &>/dev/null; then
    ufw allow 80/tcp > /dev/null 2>&1 || true
  fi

  ~/.acme.sh/acme.sh --set-default-ca --server letsencrypt --force > /dev/null 2>&1 || true

  if ! ~/.acme.sh/acme.sh --issue \
    -d "${ip}" \
    --standalone \
    --httpport 80 \
    --certificate-profile shortlived \
    --days 6 \
    --force; then
    rm -rf ~/.acme.sh/"${ip}" ~/.acme.sh/"${ip}_ecc" 2>/dev/null
    rm -rf "$CERT_DIR" 2>/dev/null
    die "Failed to obtain SSL certificate. Check that port 80 is reachable from the internet."
  fi

  ~/.acme.sh/acme.sh --installcert -d "${ip}" \
    --key-file       "${CERT_DIR}/privkey.pem" \
    --fullchain-file "${CERT_DIR}/fullchain.pem" \
    --reloadcmd      "${our_reload}" \
    2>&1 || true

  if [[ ! -f "${CERT_DIR}/fullchain.pem" || ! -f "${CERT_DIR}/privkey.pem" ]]; then
    rm -rf ~/.acme.sh/"${ip}" ~/.acme.sh/"${ip}_ecc" 2>/dev/null
    rm -rf "$CERT_DIR" 2>/dev/null
    die "Certificate files missing after install. Check acme.sh logs."
  fi

  ~/.acme.sh/acme.sh --upgrade --auto-upgrade > /dev/null 2>&1 || true

  _fix_cert_permissions
  CERT_FILE="${CERT_DIR}/fullchain.pem"
  KEY_FILE="${CERT_DIR}/privkey.pem"

  print_color "$GREEN" "Let's Encrypt IP certificate issued (valid ~6 days, auto-renews)."
}

download_release() {
  VERSION=$(curl -fsSL "https://api.github.com/repos/${GITHUB_REPO}/releases/latest" \
    | grep -Po '"tag_name": "\K[^"]+' || true)
  [ -n "$VERSION" ] || die "Could not fetch latest release version"

  mkdir -p "$DASHBOARD_DIR"
  curl -fsSL "https://github.com/${GITHUB_REPO}/releases/download/${VERSION}/release.tar.gz" \
    | tar -xz --no-same-owner -C "$DASHBOARD_DIR" \
    || die "Failed to download release ${VERSION}"
  [ -f "$CTL" ] || die "Release ${VERSION} is not compatible with this installer"
}

install_hysteria() {
  bash <(curl -fsSL https://get.hy2.sh/) >/dev/null
  command -v hysteria &>/dev/null || die "Failed to install Hysteria2"
}

configure_traffic_api_firewall() {
  if ! command -v ufw >/dev/null 2>&1; then
    echo "Warning: ufw not found. Traffic API firewall rule must be configured manually."
    return 0
  fi

  local backend_id network_id subnet hosts_path host_gateway
  local old_subnet old_host_gateway

  [ -f "$STATE_FILE" ] || die "Installation state file not found"

  docker compose -f "$COMPOSE_FILE" create --no-deps backend >/dev/null \
    || die "Failed to create backend container"

  backend_id=$(docker compose -f "$COMPOSE_FILE" ps -a -q backend | head -n1)
  [ -n "$backend_id" ] || die "Backend container not found"

  network_id=$(docker inspect "$backend_id" \
    --format '{{range $id, $net := .NetworkSettings.Networks}}{{println $id}}{{end}}' \
    | head -n1)

  [ -n "$network_id" ] || die "Backend network not found"

  subnet=$(docker network inspect "$network_id" \
    --format '{{(index .IPAM.Config 0).Subnet}}')

  hosts_path=$(docker inspect "$backend_id" --format '{{.HostsPath}}')
  [ -r "$hosts_path" ] || die "Backend hosts file not found"

  host_gateway=$(awk '$2 == "host.docker.internal" {print $1; exit}' "$hosts_path")

  [ -n "$subnet" ] || die "Docker subnet not found"
  [ -n "$host_gateway" ] || die "Docker host gateway not found"

  old_subnet=$(grep -Po '^DOCKER_SUBNET=\K.*' "$STATE_FILE" || true)
  old_host_gateway=$(grep -Po '^DOCKER_HOST_GATEWAY=\K.*' "$STATE_FILE" || true)

  if [[ -n "$old_subnet" && -n "$old_host_gateway" ]]; then
    ufw --force delete allow in from "$old_subnet" to "$old_host_gateway" port 9999 proto tcp \
      >/dev/null 2>&1 || true
  fi

  ufw allow in from "$subnet" to "$host_gateway" port 9999 proto tcp \
    || die "Failed to configure traffic API firewall rule"

  sed -i '/^DOCKER_SUBNET=/d; /^DOCKER_HOST_GATEWAY=/d' "$STATE_FILE"
  printf 'DOCKER_SUBNET=%s\nDOCKER_HOST_GATEWAY=%s\n' \
    "$subnet" "$host_gateway" >> "$STATE_FILE"

  echo "Traffic API access allowed: $subnet -> $host_gateway:9999"
}

remove_traffic_api_firewall() {
  command -v ufw >/dev/null 2>&1 || return 0
  [ -f "$STATE_FILE" ] || return 0

  local subnet host_gateway

  subnet=$(grep -Po '^DOCKER_SUBNET=\K.*' "$STATE_FILE" || true)
  host_gateway=$(grep -Po '^DOCKER_HOST_GATEWAY=\K.*' "$STATE_FILE" || true)

  if [[ -n "$subnet" && -n "$host_gateway" ]]; then
    if ufw --force delete allow in from "$subnet" to "$host_gateway" port 9999 proto tcp \
      >/dev/null 2>&1; then
      echo "Traffic API firewall rule removed."
    else
      echo "Warning: could not remove traffic API firewall rule."
    fi
  fi
}

setup_system() {
  chown -R root:root "$CONF_DIR" "$DASHBOARD_DIR"
  chgrp hysteria "$CONF_DIR" "$CONFIG_FILE"
  chmod 750 "$CONF_DIR"
  chmod 640 "$CONFIG_FILE"
  chmod 600 "$APP_JSON" "$STATE_FILE"
  chmod 755 "$CTL"

  printf 'net.core.rmem_max=16777216\nnet.core.wmem_max=16777216\n' > "$SYSCTL_FILE"
  sysctl -q -p "$SYSCTL_FILE" 2>/dev/null || echo "Warning: could not apply $SYSCTL_FILE"

  mkdir -p "$DROPIN_DIR"
  rm -f "${DROPIN_DIR}/killmode.conf"
  printf '[Service]\nKillMode=process\nEnvironment=HYSTERIA_DISABLE_UPDATE_CHECK=1\nRestart=on-failure\nRestartSec=3\nNice=-5\n' \
    > "${DROPIN_DIR}/override.conf"
  systemctl daemon-reload

  mkdir -p /etc/sudoers.d
  echo "hysteria ALL=(root) NOPASSWD: ${CTL}" > "$SUDOERS_FILE"
  chmod 440 "$SUDOERS_FILE"
}

cmd_install() {
  echo "Downloading Dashboard files..."
  download_release
  echo "Done."

  SERVER_IP=$(curl -4 -fs --max-time 5 https://api.ipify.org || curl -4 -fs --max-time 5 https://ifconfig.me || true)
  valid_ipv4 "$SERVER_IP" || die "Could not detect external IP"

  echo ""
  echo "Hysteria2 + Dashboard Installer"
  echo "Leave blank to keep defaults"
  echo ""

  while true; do
    read -rp "Hysteria2 port [443]: " _in; H2_PORT="${_in:-443}"
    if ! valid_port "$H2_PORT"; then
      print_color "$YELLOW" "Port must be a number between 1 and 65535."
    elif ! udp_port_free "$H2_PORT"; then
      print_color "$YELLOW" "UDP port $H2_PORT is already in use."
    else
      break
    fi
  done
  while true; do
    read -rp "SNI (blank = verify server certificate) []: " SNI
    if valid_sni "$SNI"; then
      break
    else
      print_color "$YELLOW" "SNI must be empty or a host name."
    fi
  done
  while true; do
    read -rp "First username [user]: " _in; FIRST_USER="${_in:-user}"; FIRST_USER="${FIRST_USER,,}"
    if valid_user "$FIRST_USER"; then
      break
    else
      print_color "$YELLOW" "Username must be 1-32 characters: a-z, 0-9, _ or -."
    fi
  done
  while true; do
    read -rp "Dashboard port [443]: " _in; DASH_PORT="${_in:-443}"
    if ! valid_port "$DASH_PORT"; then
      print_color "$YELLOW" "Port must be a number between 1 and 65535."
    elif ! tcp_port_free "$DASH_PORT"; then
      print_color "$YELLOW" "TCP port $DASH_PORT is already in use."
    else
      break
    fi
  done
  while true; do
    read -rp "Dashboard admin password: " ADMIN_PASS
    echo
    if [ -z "$ADMIN_PASS" ] || [ "${#ADMIN_PASS}" -gt 64 ]; then
      print_color "$YELLOW" "Password must be 1-64 characters."
    else
      break
    fi
  done

  KEY_NAME="Hysteria2"
  OBFS_PASS=$(rand 32)
  FIRST_PASS=$(rand 32)
  JWT_SECRET=$(rand_b64 64)
  TRAFFIC_SECRET=$(rand 32)
  ROUTE_SALT=$(rand 16)

  echo "Installing Hysteria2..."
  install_hysteria
  echo "Done."

  mkdir -p "$CONF_DIR"

  cat > "$STATE_FILE" <<STATE
INSTALLED_DOCKER=0
INSTALLED_SUDO=0
INSTALLED_BCRYPT=0
INSTALLED_ACME=0
INSTALL_COMPLETE=0
STATE

  echo "Setting up TLS certificate..."
  CERT_FILE=""
  KEY_FILE=""
  setup_ssl_cert "$SERVER_IP"
  echo "Done."

  echo "Writing Hysteria2 config..."
  cat > "$CONFIG_FILE" <<CONF
listen: :${H2_PORT}

tls:
  cert: ${CERT_FILE}
  key: ${KEY_FILE}

ignoreClientBandwidth: true

auth:
  type: userpass
  userpass:
    "${FIRST_USER}": "${FIRST_PASS}"

obfs:
  type: salamander
  salamander:
    password: "${OBFS_PASS}"

acl:
  inline:
    - reject(0.0.0.0/8)
    - reject(10.0.0.0/8)
    - reject(100.64.0.0/10)
    - reject(127.0.0.0/8)
    - reject(169.254.0.0/16)
    - reject(172.16.0.0/12)
    - reject(192.168.0.0/16)
    - reject(::/128)
    - reject(::1/128)
    - reject(fc00::/7)
    - reject(fe80::/10)
    - reject(all, tcp/25)

trafficStats:
  listen: :9999
  secret: "${TRAFFIC_SECRET}"
CONF
  echo "Done."

  echo "Hashing admin password..."
  if ! python3 -c "import bcrypt" 2>/dev/null; then
    apt-get update -qq >/dev/null 2>&1 || true
    apt-get install -y -qq python3-bcrypt >/dev/null
    state_set INSTALLED_BCRYPT 1
  fi

  ADMIN_HASH=$(python3 -c 'import bcrypt, sys; print(bcrypt.hashpw(sys.argv[1].encode(), bcrypt.gensalt()).decode())' "$ADMIN_PASS")
  [ -n "$ADMIN_HASH" ] || die "Failed to hash admin password"
  echo "Done."

  echo "Writing app.json..."
  cat > "$APP_JSON" <<JSON
{
  "jwtSecret": "${JWT_SECRET}",
  "trafficApiSecret": "${TRAFFIC_SECRET}",
  "adminPasswordHash": "${ADMIN_HASH}",
  "serverIP": "${SERVER_IP}",
  "keyName": "${KEY_NAME}",
  "sni": "${SNI}",
  "routeSalt": "${ROUTE_SALT}"
}
JSON
  echo "Done."

  echo "Configuring system..."
  setup_system
  echo "Done."

  echo "Configuring firewall..."
  if command -v ufw &>/dev/null; then
    ufw allow "${H2_PORT}/udp" >/dev/null 2>&1 || true
    ufw allow "${DASH_PORT}/tcp" >/dev/null 2>&1 || true
    echo "Done."
  else
    echo "Warning: ufw not found. Open ports ${H2_PORT}/udp and ${DASH_PORT}/tcp manually."
  fi

  echo "Setting up SSH access for Dashboard..."
  if ! command -v sudo &>/dev/null; then
    apt-get update -qq >/dev/null 2>&1 || true
    apt-get install -y -qq sudo >/dev/null
    state_set INSTALLED_SUDO 1
  fi

  SSH_PORT=$(grep -Po '^Port \K\d+' /etc/ssh/sshd_config 2>/dev/null || echo "22")

  rm -f "${DASHBOARD_DIR}/ssh_key" "${DASHBOARD_DIR}/ssh_key.pub"
  ssh-keygen -q -t ed25519 -f "${DASHBOARD_DIR}/ssh_key" -N ""
  mkdir -p /var/lib/hysteria/.ssh
  cat "${DASHBOARD_DIR}/ssh_key.pub" > /var/lib/hysteria/.ssh/authorized_keys
  chown -R hysteria:hysteria /var/lib/hysteria/.ssh
  chmod 700 /var/lib/hysteria/.ssh
  chmod 600 /var/lib/hysteria/.ssh/authorized_keys
  chmod 600 "${DASHBOARD_DIR}/ssh_key"
  echo "Done."

  echo "Starting Hysteria2..."
  systemctl enable hysteria-server >/dev/null 2>&1
  systemctl restart hysteria-server
  local attempts=0
  until systemctl is-active --quiet hysteria-server || [[ $attempts -ge 10 ]]; do
    sleep 1
    attempts=$((attempts + 1))
  done
  systemctl is-active --quiet hysteria-server \
    || die "Hysteria2 failed. Check: journalctl -u hysteria-server -n 50"
  echo "Done."

  if ! command -v docker &>/dev/null; then
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com | sh >/dev/null 2>&1 \
      || die "Failed to install Docker"
    state_set INSTALLED_DOCKER 1

    systemctl enable docker >/dev/null 2>&1
    systemctl start docker
    echo "Done."
  else
    echo "Docker already installed, skipping."
  fi

  echo "Setting up Dashboard..."
  cat > "$COMPOSE_ENV" <<ENV
ROUTE_SALT=${ROUTE_SALT}
DASH_PORT=${DASH_PORT}
SSH_PORT=${SSH_PORT}
GITHUB_REPO=${GITHUB_REPO}
VERSION=${VERSION}
ENV

  chmod 600 "$COMPOSE_ENV"
  mkdir -p "${DASHBOARD_DIR}/data"
  echo "Done."

  echo "Starting Dashboard..."
  docker compose -f "$COMPOSE_FILE" pull
  configure_traffic_api_firewall
  docker compose -f "$COMPOSE_FILE" up -d
  echo "Done."

  state_set INSTALL_COMPLETE 1

  echo ""
  print_color "$GREEN" "Installation complete!"
  echo ""
  echo "Dashboard:"
  echo "  URL: https://${SERVER_IP}:${DASH_PORT}/${ROUTE_SALT}/"
  echo "  Password: $ADMIN_PASS"
  echo ""
}

cmd_update() {
  echo "Downloading Dashboard files..."
  download_release
  echo "Done."

  echo "Updating Hysteria2..."
  install_hysteria
  echo "Done."

  echo "Configuring system..."
  setup_system
  mkdir -p "${DASHBOARD_DIR}/data"
  sed -i "s/^VERSION=.*/VERSION=${VERSION}/" "$COMPOSE_ENV"
  echo "Done."

  echo "Updating Dashboard..."
  docker compose -f "$COMPOSE_FILE" pull
  remove_traffic_api_firewall
  configure_traffic_api_firewall
  docker compose -f "$COMPOSE_FILE" up -d
  echo "Done."

  echo "Restarting Hysteria2..."
  local attempts=0
  until grep -q '^acl:' "$CONFIG_FILE" || [[ $attempts -ge 30 ]]; do
    sleep 1
    attempts=$((attempts + 1))
  done
  "$CTL" restart
  echo "Done."

  echo ""
  print_color "$GREEN" "Update complete! Version: ${VERSION}"
  echo ""
}

cmd_uninstall() {
  read -rp "Remove Hysteria2, Dashboard and all data? [y/N]: " _in
  [ "${_in,,}" != "y" ] && { echo "Aborted."; exit 0; }

  local installed_docker installed_sudo installed_bcrypt installed_acme
  installed_docker=$(state_get INSTALLED_DOCKER)
  installed_sudo=$(state_get INSTALLED_SUDO)
  installed_bcrypt=$(state_get INSTALLED_BCRYPT)
  installed_acme=$(state_get INSTALLED_ACME)

  local h2_port="" dash_port=""
  if [ -f "$CONFIG_FILE" ]; then
    h2_port=$(grep -Po '^listen: :\K\d+' "$CONFIG_FILE" || true)
  fi
  if [ -f "$COMPOSE_ENV" ]; then
    dash_port=$(grep -Po '^DASH_PORT=\K\d+' "$COMPOSE_ENV" || true)
  fi

  echo "Stopping Hysteria2 service..."
  systemctl stop hysteria-server 2>/dev/null || true
  systemctl disable hysteria-server 2>/dev/null || true
  echo "Done."

  echo "Removing Hysteria2..."
  bash <(curl -fsSL https://get.hy2.sh/) --remove >/dev/null 2>&1 || true
  rm -f /etc/systemd/system/multi-user.target.wants/hysteria-server.service
  rm -f /etc/systemd/system/multi-user.target.wants/hysteria-server@*.service
  rm -rf "$DROPIN_DIR"
  rm -f "$SYSCTL_FILE" "$SUDOERS_FILE"
  if id "hysteria" &>/dev/null; then
    userdel -r hysteria 2>/dev/null || userdel hysteria 2>/dev/null || true
    rm -rf /var/lib/hysteria
  fi
  systemctl daemon-reload
  echo "Done."

  echo "Removing Dashboard containers and files..."
  if [ -f "$COMPOSE_FILE" ]; then
    docker compose -f "$COMPOSE_FILE" down -v --rmi all 2>/dev/null || true
  fi
  docker image prune -f >/dev/null 2>&1 || true
  rm -rf "$DASHBOARD_DIR"
  echo "Done."

  echo "Removing firewall rules..."
  if command -v ufw &>/dev/null; then
    [ -n "$h2_port" ]   && { ufw delete allow "${h2_port}/udp"   >/dev/null 2>&1 || true; }
    [ -n "$dash_port" ] && { ufw delete allow "${dash_port}/tcp" >/dev/null 2>&1 || true; }
    ufw delete allow 80/tcp >/dev/null 2>&1 || true
    echo "Done."
  else
    echo "Warning: ufw not found. Remove firewall rules manually."
  fi

  echo "Removing traffic API firewall rule..."
  remove_traffic_api_firewall

  echo "Removing config and certificates..."
  rm -rf "$CONF_DIR"
  echo "Done."

  if [[ "$installed_acme" == "1" ]]; then
    echo "Removing acme.sh and certificates..."
    ~/.acme.sh/acme.sh --uninstall > /dev/null 2>&1 || true
    rm -rf ~/.acme.sh
    rm -rf "$CERT_DIR"
    echo "Done."
  else
    echo "acme.sh was pre-installed, skipping removal."
  fi

  if [[ "$installed_docker" == "1" ]]; then
    echo "Removing Docker..."
    systemctl stop docker 2>/dev/null || true
    systemctl disable docker 2>/dev/null || true
    apt-get remove -y -qq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin 2>/dev/null || true
    apt-get autoremove -y -qq 2>/dev/null || true
    rm -rf /var/lib/docker /etc/docker /opt/containerd
    echo "Done."
  else
    echo "Docker was pre-installed, skipping removal."
  fi

  if [[ "$installed_sudo" == "1" ]]; then
    echo "Removing sudo..."
    apt-get remove -y -qq sudo 2>/dev/null || true
    echo "Done."
  fi

  if [[ "$installed_bcrypt" == "1" ]]; then
    echo "Removing python3-bcrypt..."
    apt-get remove -y -qq python3-bcrypt 2>/dev/null || true
    echo "Done."
  fi

  echo ""
  print_color "$GREEN" "Hysteria2 and Dashboard removed."
  echo ""
}

exec </dev/tty

if [ -f "$STATE_FILE" ] && grep -q "^INSTALL_COMPLETE=1" "$STATE_FILE"; then
  echo "Dashboard is already installed."
  echo "  1  Update"
  echo "  2  Uninstall"
  read -rp "Choose [1/2]: " _in
  case "$_in" in
    1) cmd_update ;;
    2) cmd_uninstall ;;
    *) echo "Aborted." ;;
  esac
else
  cmd_install
fi
