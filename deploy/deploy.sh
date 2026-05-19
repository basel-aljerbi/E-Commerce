#!/usr/bin/env bash
set -euo pipefail

# ============================================================
# E-Commerce Deployment Script
# Called by GitHub Actions after pushing new images
# ============================================================

DEPLOY_DIR="/opt/ecommerce"
cd "$DEPLOY_DIR"

echo "==> Pulling latest Docker images..."
docker compose pull

echo "==> Recreating containers with new images..."
docker compose up -d --remove-orphans

echo "==> Removing unused images and volumes..."
docker image prune -f

echo "==> Checking container health..."
sleep 10

if curl -sf http://localhost:8080/health > /dev/null 2>&1; then
    echo "✅ Backend health check passed"
else
    echo "⚠️  Backend health check failed — check logs: docker compose logs backend"
fi

echo "==> Deployment complete!"
