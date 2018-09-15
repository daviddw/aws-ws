#!/usr/bin/env bash
set -e

cd /app

exec dotnet aws-service-test.dll
