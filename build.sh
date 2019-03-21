#! /bin/bash

set -e

# Build
dotnet build -c Release

# Unit Test
dotnet test -c Release --no-build

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
