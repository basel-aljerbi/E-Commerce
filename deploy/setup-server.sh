#!/usr/bin/env bash
set -euo pipefail

# ============================================================
# E-Commerce Production Server Setup Script
# Run ONCE on a fresh Ubuntu 24.04 VPS
# ============================================================

echo "==> Updating system packages..."
sudo apt-get update -qq
sudo apt-get upgrade -y -qq

echo "==> Installing Docker..."
sudo apt-get install -y -qq ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update -qq
sudo apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin

echo "==> Adding current user to docker group..."
sudo usermod -aG docker "$USER"

echo "==> Creating deployment directory..."
sudo mkdir -p /opt/ecommerce
sudo chown "$USER:$USER" /opt/ecommerce

echo "==> Setting up UFW firewall..."
sudo ufw --force reset
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 22/tcp
sudo ufw --force enable

echo "==> Enabling Docker on boot..."
sudo systemctl enable docker

echo ""
echo "============================================"
echo "  Server setup complete!"
echo ""
echo "  Next steps:"
echo "  1. Log out and back in (for docker group)"
echo "  2. cd /opt/ecommerce"
echo "  3. Copy deploy files to this directory"
echo "  4. Create .env.production with secrets"
echo "  5. Run: docker compose up -d"
echo "============================================"
