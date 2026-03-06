# Docker Image Build & Push Anleitung

## Voraussetzungen

### GitHub Personal Access Token erstellen

1. Gehe zu: `https://github.com/settings/tokens`
2. Klicke auf **"Generate new token"** → **"Generate new token (classic)"**
3. Wähle diese Scopes:
   - `write:packages`
   - `read:packages`
4. Token kopieren und sicher aufbewahren

### Bei GitHub Container Registry anmelden

```bash
docker login ghcr.io -u ernstbe
# Bei Password: den Token eingeben (nicht das GitHub-Passwort)
```

## Image bauen und pushen

### 1. Im Trudesk-Ordner das Image bauen

```bash
docker build -t ghcr.io/ernstbe/trudesk:latest .
```

### 2. Image zu GitHub Container Registry pushen

```bash
docker push ghcr.io/ernstbe/trudesk:latest
```

### 3. Mit Version-Tag (empfohlen für Produktion)

```bash
docker build -t ghcr.io/ernstbe/trudesk:1.0.0 .
docker push ghcr.io/ernstbe/trudesk:1.0.0
```

## Image verwenden

In `docker-compose.yml`:

```yaml
services:
  trudesk:
    image: ghcr.io/ernstbe/trudesk:latest
    # ... restliche Konfiguration
```

Dann:

```bash
docker-compose pull trudesk
docker-compose up -d trudesk
```

## Automatischer Build mit GitHub Actions (optional)

Erstelle `.github/workflows/docker.yml` im Trudesk-Fork:

```yaml
name: Build Docker Image

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4

      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ghcr.io/${{ github.repository }}:latest
            ghcr.io/${{ github.repository }}:${{ github.sha }}
```

Damit wird bei jedem Push auf `main` automatisch ein neues Image gebaut und veröffentlicht.
