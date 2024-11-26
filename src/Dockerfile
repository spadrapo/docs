FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebDocs/WebDocs.csproj", "WebDocs/"]
RUN dotnet restore "WebDocs/WebDocs.csproj"
COPY . .
WORKDIR "/src/WebDocs"
RUN dotnet build "WebDocs.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebDocs.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebDocs.dll"]