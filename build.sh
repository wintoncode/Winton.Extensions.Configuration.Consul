set -e
dotnet restore
cd src/Winton.Extensions.Configuration.Consul
dotnet gitversion
cd ../../
dotnet build src/Winton.Extensions.Configuration.Consul/project.json test/Winton.Extensions.Configuration.Consul.Test/project.json --configuration Release
dotnet test --no-build --configuration Release -f netcoreapp1.0 test/Winton.Extensions.Configuration.Consul.Test/project.json
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
dotnet pack --no-build src/*/project.json --configuration Release