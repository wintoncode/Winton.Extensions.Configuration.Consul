FROM mcr.microsoft.com/dotnet/core/aspnet:3.0

COPY bin/Release/netcoreapp3.0/publish /app
WORKDIR /app
 
EXPOSE 80/tcp
ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "Winton.Extensions.Configuration.Consul.Test.Website.dll"]
