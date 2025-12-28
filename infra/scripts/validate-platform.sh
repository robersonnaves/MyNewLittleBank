#!/usr/bin/env bash
set -euo pipefail

# Colors
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${CYAN}╔════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  Platform Validation                   ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════╝${NC}"
echo ""

# System Information
echo -e "${CYAN}System Information:${NC}"
echo -e "  OS: $(uname -s)"
echo -e "  Kernel: $(uname -r)"
echo -e "  Architecture: $(uname -m)"
echo ""

# .NET SDK
echo -e "${CYAN}.NET SDK:${NC}"
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "  ${GREEN}✓${NC} .NET SDK: ${DOTNET_VERSION}"
    
    if [[ "${DOTNET_VERSION}" == 8.0.* ]] || [[ "${DOTNET_VERSION}" == 9.0.* ]]; then
        echo -e "  ${GREEN}✓${NC} Compatible version"
    else
        echo -e "  ${YELLOW}⚠${NC} Expected .NET 8.0 or 9.0"
    fi
else
    echo -e "  ${RED}✗${NC} .NET SDK not found"
    echo -e "  ${YELLOW}→${NC} Install from: https://dotnet.microsoft.com/download"
fi
echo ""

# Go (optional)
echo -e "${CYAN}Go (optional):${NC}"
if command -v go &> /dev/null; then
    GO_VERSION=$(go version | awk '{print $3}')
    echo -e "  ${GREEN}✓${NC} Go: ${GO_VERSION}"
else
    echo -e "  ${YELLOW}⚠${NC} Go not found (needed only for local dev of notification-receiver)"
fi
echo ""

# Container Engine
echo -e "${CYAN}Container Engine:${NC}"
ENGINE_FOUND=false

if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | awk '{print $3}' | tr -d ',')
    echo -e "  ${GREEN}✓${NC} Docker: ${DOCKER_VERSION}"
    ENGINE_FOUND=true
    
    if docker compose version &> /dev/null; then
        COMPOSE_VERSION=$(docker compose version --short)
        echo -e "  ${GREEN}✓${NC} Docker Compose: ${COMPOSE_VERSION}"
    else
        echo -e "  ${YELLOW}⚠${NC} Docker Compose not found"
    fi
    
    if docker buildx version &> /dev/null; then
        echo -e "  ${GREEN}✓${NC} Docker Buildx: available"
    else
        echo -e "  ${YELLOW}⚠${NC} Docker Buildx not found (needed for multi-arch builds)"
    fi
fi

if command -v podman &> /dev/null; then
    PODMAN_VERSION=$(podman --version | awk '{print $3}')
    echo -e "  ${GREEN}✓${NC} Podman: ${PODMAN_VERSION}"
    ENGINE_FOUND=true
    
    if podman compose version &> /dev/null 2>&1; then
        echo -e "  ${GREEN}✓${NC} Podman Compose: available"
    else
        echo -e "  ${YELLOW}⚠${NC} Podman Compose not found"
    fi
    
    if podman build --help | grep -q "platform"; then
        echo -e "  ${GREEN}✓${NC} Multi-arch support: available"
    else
        echo -e "  ${RED}✗${NC} Multi-arch support: not available (upgrade to Podman 3.0+)"
    fi
fi

if [[ "${ENGINE_FOUND}" == "false" ]]; then
    echo -e "  ${RED}✗${NC} No container engine found"
    echo -e "  ${YELLOW}→${NC} Install Docker or Podman"
fi
echo ""

# Architecture Test
echo -e "${CYAN}Architecture Test:${NC}"
ENGINE="${CONTAINER_ENGINE:-podman}"

if [[ "${ENGINE_FOUND}" == "true" ]]; then
    if ${ENGINE} run --rm --platform linux/arm64 alpine:latest uname -m &> /dev/null; then
        RESULT=$(${ENGINE} run --rm --platform linux/arm64 alpine:latest uname -m)
        echo -e "  ${GREEN}✓${NC} Can run ARM64 containers: ${RESULT}"
    else
        echo -e "  ${RED}✗${NC} Cannot run ARM64 containers"
    fi
    
    if ${ENGINE} run --rm --platform linux/amd64 alpine:latest uname -m &> /dev/null; then
        RESULT=$(${ENGINE} run --rm --platform linux/amd64 alpine:latest uname -m)
        echo -e "  ${GREEN}✓${NC} Can run AMD64 containers: ${RESULT}"
    else
        echo -e "  ${YELLOW}⚠${NC} Cannot run AMD64 containers (emulation may not be available)"
    fi
fi
echo ""

# Summary
echo -e "${CYAN}╔════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  Validation Complete                   ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════╝${NC}"
echo ""

ERRORS=0

if ! command -v dotnet &> /dev/null; then
    ((ERRORS++))
fi

if [[ "${ENGINE_FOUND}" == "false" ]]; then
    ((ERRORS++))
fi

if [[ ${ERRORS} -gt 0 ]]; then
    echo -e "${RED}✗ ${ERRORS} critical issue(s) found${NC}"
    echo -e "${RED}Cannot proceed without fixing these issues${NC}"
    exit 1
else
    echo -e "${GREEN}✓ All checks passed!${NC}"
    echo -e "${GREEN}Your environment is ready for multi-platform development${NC}"
    exit 0
fi
