#!/bin/bash
set -e # stop on error
docker buildx build \
    --platform linux/arm/v7 \
    -t leepaulalexander/allotment:latest \
    -f ./allotment/Dockerfile \
    --push \
    .
