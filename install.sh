#!/bin/bash

set -euo pipefail

[ "$EUID" -ne 0 ] && { echo "Error: run as root"; exit 1; }

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

GITHUB_REPO="sodiumcxi/hysteria2-dashboard"
CONFIG_FILE="/etc/hysteria/config.yaml"
APP_JSON="/etc/hysteria/app.json"
DASHBOARD_DIR="/opt/hysteria2-dashboard"
COMPOSE_ENV="${DASHBOARD_DIR}/.env"
STATE_FILE="/etc/hysteria/install_state"
CERT_DIR="/root/cert/ip"

rand()  { tr -dc 'A-Za-z0-9' </dev/urandom | head -c "$1"; true; }
rand_b64() { tr -dc 'A-Za-z0-9+/' </dev/urandom | head -c "$1"; true; }
die()   { echo "Error: $*"; exit 1; }

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

run_until() {
  local cmd="$1"
  local needle="$2"
  script -qefc "$cmd" /dev/null |
    python3 -u -c "
import sys
needle = b'$needle'
line = b''
while True:
    ch = sys.stdin.buffer.read(1)
    if not ch:
        if line:
            sys.stdout.buffer.write(line)
            sys.stdout.buffer.flush()
        break
    line += ch
    if ch in (b'\n', b'\r'):
        sys.stdout.buffer.write(line)
        sys.stdout.buffer.flush()
        if needle in line:
            sys.stdout.buffer.write(b'\n')
            sys.stdout.buffer.flush()
            break
        line = b''
"
}

state_set() {
  local key="$1"
  local value="$2"
  sed -i "s/^${key}=.*/${key}=${value}/" "$STATE_FILE"
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
 
  local our_reload="systemctl restart hysteria-server 2>/dev/null; docker compose -f /opt/hysteria2-dashboard/docker-compose.yml restart 2>/dev/null"
 
  if [[ -f "${CERT_DIR}/fullchain.pem" && -f "${CERT_DIR}/privkey.pem" ]]; then
    echo "Found existing certificate in ${CERT_DIR} — reusing it."

    local conf_file=~/.acme.sh/"${ip}_ecc"/"${ip}.conf"
    local existing_reload=""
    if [[ -f "$conf_file" ]]; then
      local encoded
      encoded=$(grep 'Le_ReloadCmd=' "$conf_file" \
        | sed "s/Le_ReloadCmd='__ACME_BASE64__START_\(.*\)__ACME_BASE64__END_'/\1/")
      if [[ -n "$encoded" ]]; then
        existing_reload=$(echo "$encoded" | base64 -d)
      fi
    fi

    local final_reload="${our_reload}"
    if [[ -n "$existing_reload" ]]; then
      final_reload="${our_reload}; ${existing_reload}"
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
    ufw allow 80/tcp > /dev/null 2>&1
  fi

  ~/.acme.sh/acme.sh --set-default-ca --server letsencrypt --force > /dev/null 2>&1

  ~/.acme.sh/acme.sh --issue \
    -d "${ip}" \
    --standalone \
    --httpport 80 \
    --certificate-profile shortlived \
    --days 6 \
    --force

  if [[ $? -ne 0 ]]; then
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

  ~/.acme.sh/acme.sh --upgrade --auto-upgrade > /dev/null 2>&1

  _fix_cert_permissions
  CERT_FILE="${CERT_DIR}/fullchain.pem"
  KEY_FILE="${CERT_DIR}/privkey.pem"
  
  print_color "$GREEN" "Let's Encrypt IP certificate issued (valid ~6 days, auto-renews)."
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
    read -rp "Hysteria2 port [443]: " _in; H2_PORT="${_in:-443}"
    if udp_port_free "$H2_PORT"; then
      break
    else
	  print_color "$YELLOW" "UDP port $H2_PORT is already in use."
    fi
  done
  read -rp "SNI [google.com]: " _in; SNI="${_in:-google.com}"
  read -rp "First username [FirstUser]: " _in; FIRST_USER="${_in:-FirstUser}"
  while true; do
    read -rp "Dashboard port [443]: " _in; DASH_PORT="${_in:-443}"
    if tcp_port_free "$DASH_PORT"; then
      break
    else
      print_color "$YELLOW" "TCP port $DASH_PORT is already in use."
    fi
  done
  while true; do
    read -rp "Dashboard admin password: " ADMIN_PASS
    echo
    if [ -z "$ADMIN_PASS" ]; then
      print_color "$YELLOW" "Password cannot be empty."
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

  export PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"

  echo "Installing Hysteria2..."

  set +o pipefail
  run_until 'bash <(curl -fsSL https://get.hy2.sh/)' 'Congratulation'
  stty sane 2>/dev/null || true
  set -o pipefail

  mkdir -p /etc/hysteria
  chown hysteria:hysteria /etc/hysteria
  chmod 750 /etc/hysteria
  
  cat > "$STATE_FILE" <<STATE
INSTALLED_DOCKER=0
INSTALLED_SUDO=0
INSTALLED_BCRYPT=0
INSTALLED_ACME=0
INSTALL_COMPLETE=0
STATE

  echo "Setting up TLS certificate..."
  mkdir -p /etc/hysteria
  chown hysteria:hysteria /etc/hysteria
  chmod 750 /etc/hysteria
 
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
	state_set INSTALLED_SUDO 1
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
	
    set +o pipefail
    run_until 'curl -fsSL https://get.docker.com | sh' 'INFO'
    stty sane 2>/dev/null || true
    set -o pipefail
	
	state_set INSTALLED_DOCKER 1
	
    systemctl enable docker >/dev/null 2>&1
    systemctl start docker
    echo "Done."
  else
    echo "Docker already installed, skipping."
  fi

  echo "Hashing admin password..."

  if ! python3 -c "import bcrypt" 2>/dev/null; then
    apt install -y -qq python3-bcrypt
	state_set INSTALLED_BCRYPT 1
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
  print_color "$GREEN" "Installation complete!"
  echo ""
  
  state_set INSTALL_COMPLETE 1
  
  echo "Dashboard:"
  echo "  URL: https://${SERVER_IP}:${DASH_PORT}/${ROUTE_SALT}"
  echo "  Password: $ADMIN_PASS"
  echo ""
}

cmd_uninstall() {
  exec </dev/tty

  read -rp "Remove Hysteria2, Dashboard and all data? [y/N]: " _in
  [ "${_in,,}" != "y" ] && { echo "Aborted."; exit 0; }

  local INSTALLED_DOCKER=0
  local INSTALLED_SUDO=0
  local INSTALLED_BCRYPT=0
  local INSTALLED_ACME=0
  [ -f "$STATE_FILE" ] && source "$STATE_FILE"

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
    ufw delete allow 80/tcp >/dev/null 2>&1 || true
    echo "Done."
  else
    echo "Warning: ufw not found. Remove firewall rules manually."
  fi

  if [[ "$INSTALLED_ACME" == "1" ]]; then
    echo "Removing acme.sh and certificates..."
    ~/.acme.sh/acme.sh --uninstall > /dev/null 2>&1 || true
    rm -rf ~/.acme.sh
    rm -rf /root/cert/ip
    echo "Done."
  else
    echo "acme.sh was pre-installed, skipping removal."
  fi

  if [[ "$INSTALLED_DOCKER" == "1" ]]; then
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

  if [[ "$INSTALLED_SUDO" == "1" ]]; then
    echo "Removing sudo..."
    apt-get remove -y -qq sudo 2>/dev/null || true
    echo "Done."
  fi

  if [[ "$INSTALLED_BCRYPT" == "1" ]]; then
    echo "Removing python3-bcrypt..."
    apt-get remove -y -qq python3-bcrypt 2>/dev/null || true
    echo "Done."
  fi

  echo ""
  print_color "$GREEN" "Hysteria2 and Dashboard removed."
  echo ""
}

if [ -f "$STATE_FILE" ] && grep -q "^INSTALL_COMPLETE=1" "$STATE_FILE"; then
  echo "Dashboard is already installed."
  cmd_uninstall
  exit 0
else
  cmd_install
  exit 0
fi