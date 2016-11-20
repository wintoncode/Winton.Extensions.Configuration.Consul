exec { & dotnet restore }
cd src\AspNetCore.Configuration.Consul
exec { & dotnet gitversion }
cd ..\..\
exec { & dotnet build src\*\project.json test\*\project.json --configuration Release }
exec { & dotnet test --no-build --configuration Release -f netcoreapp1.0 test\*\project.json }
exec { & dotnet pack --no-build src\*\project.json --configuration Release }