FROM microsoft/dotnet:1.1.4-runtime

COPY bin/Release/netcoreapp1.1/publish /app
WORKDIR /app
 
EXPOSE 80/tcp
ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "Winton.Extensions.Configuration.Consul.Test.Website.dll"]
