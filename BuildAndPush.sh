#!/bin/bash 
set -e # stop on error
docker build    \
    --build-arg IMAGE_TAG_APPEND="-bullseye-slim-arm32v7" \
    -t leepaulalexander/allotment:latest \
    -f ./allotment/Dockerfile \
    .
docker push leepaulalexander/allotment:latest