dotnet restore
cd src/AspNetCore.Configuration.Consul
dotnet gitversion
cd ../../
dotnet build src/*/project.json test/*/project.json --configuration Release
dotnet test --no-build --configuration Release -f netcoreapp1.0 test/*/project.json
dotnet pack --no-build src/*/project.json --configuration Release
nuget push src/AspNetCore.Configuration.Consul/bin/Release/*.nupkg -ApiKey 40a11e65-c7f8-47cb-bfb9-c86ccd5e8234 -NonInteractive -Verbosity Detailed