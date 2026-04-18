#!/bin/bash
set -e # stop on error
GIT_COMMIT=$(git rev-parse --short HEAD)
echo "Building with commit: $GIT_COMMIT"
podman build \
    --platform linux/arm/v7 \
    -t leepaulalexander/allotment:latest \
    -f ./allotment/Dockerfile \
    --build-arg GIT_COMMIT=$GIT_COMMIT \
    .
podman push leepaulalexander/allotment:latest
