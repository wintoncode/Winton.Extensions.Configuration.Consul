FROM microsoft/dotnet:2.2-aspnetcore-runtime

COPY bin/Release/netcoreapp2.2/publish /app
WORKDIR /app
 
EXPOSE 80/tcp
ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "Winton.Extensions.Configuration.Consul.Test.Website.dll"]
