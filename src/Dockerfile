FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
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