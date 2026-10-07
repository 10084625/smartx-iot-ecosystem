FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SmartX.Api/*.csproj SmartX.Api/
RUN dotnet restore SmartX.Api/SmartX.Api.csproj
COPY SmartX.Api/ SmartX.Api/
RUN dotnet publish SmartX.Api/SmartX.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080
ENTRYPOINT ["dotnet", "SmartX.Api.dll"]
