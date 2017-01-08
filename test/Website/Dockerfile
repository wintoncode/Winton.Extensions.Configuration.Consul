FROM microsoft/dotnet:1.0-runtime

COPY bin/Release/netcoreapp1.0/publish /app
WORKDIR /app
 
EXPOSE 80/tcp
ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "Website.dll"]