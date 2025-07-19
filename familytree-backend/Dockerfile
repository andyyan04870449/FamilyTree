FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["familytree-backend.csproj", "./"]
RUN dotnet restore "familytree-backend.csproj"
COPY . .
RUN dotnet build "familytree-backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "familytree-backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "familytree-backend.dll"] 