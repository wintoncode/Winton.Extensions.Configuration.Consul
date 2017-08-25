#! /bin/bash

set -e

# Restore packages
dotnet restore

# Build
dotnet build --configuration Release

# Unit Test
pushd test/Winton.Extensions.Configuration.Consul.Test
dotnet test --no-build --configuration Release
popd

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
