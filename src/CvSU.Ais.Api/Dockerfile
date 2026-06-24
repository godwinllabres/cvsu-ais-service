# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project graph first for better layer caching.
COPY CvSU.Ais.slnx ./
COPY src/CvSU.Ais.Domain/CvSU.Ais.Domain.csproj src/CvSU.Ais.Domain/
COPY src/CvSU.Ais.Application/CvSU.Ais.Application.csproj src/CvSU.Ais.Application/
COPY src/CvSU.Ais.Infrastructure/CvSU.Ais.Infrastructure.csproj src/CvSU.Ais.Infrastructure/
COPY src/CvSU.Ais.Api/CvSU.Ais.Api.csproj src/CvSU.Ais.Api/
RUN dotnet restore src/CvSU.Ais.Api/CvSU.Ais.Api.csproj

COPY . .
RUN dotnet publish src/CvSU.Ais.Api/CvSU.Ais.Api.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CvSU.Ais.Api.dll"]
