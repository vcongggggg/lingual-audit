FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Copy csproj files for layer caching
COPY src/backend/Lingual.SharedKernel/*.csproj src/backend/Lingual.SharedKernel/
COPY src/backend/Lingual.Persistence/*.csproj src/backend/Lingual.Persistence/
COPY src/backend/Lingual.Infrastructure/*.csproj src/backend/Lingual.Infrastructure/
COPY src/backend/Lingual.Modules.Identity.Contracts/*.csproj src/backend/Lingual.Modules.Identity.Contracts/
COPY src/backend/Lingual.Modules.Identity/*.csproj src/backend/Lingual.Modules.Identity/
COPY src/backend/Lingual.Modules.Learning.Contracts/*.csproj src/backend/Lingual.Modules.Learning.Contracts/
COPY src/backend/Lingual.Modules.Learning/*.csproj src/backend/Lingual.Modules.Learning/
COPY src/backend/Lingual.Modules.Gamification.Contracts/*.csproj src/backend/Lingual.Modules.Gamification.Contracts/
COPY src/backend/Lingual.Modules.Gamification/*.csproj src/backend/Lingual.Modules.Gamification/
COPY src/backend/Lingual.Modules.MezonBot.Contracts/*.csproj src/backend/Lingual.Modules.MezonBot.Contracts/
COPY src/backend/Lingual.Modules.MezonBot/*.csproj src/backend/Lingual.Modules.MezonBot/
COPY src/backend/Lingual.Modules.AITutor.Contracts/*.csproj src/backend/Lingual.Modules.AITutor.Contracts/
COPY src/backend/Lingual.Modules.AITutor/*.csproj src/backend/Lingual.Modules.AITutor/
COPY src/backend/Lingual.Api/*.csproj src/backend/Lingual.Api/

RUN dotnet restore src/backend/Lingual.Api/Lingual.Api.csproj

# Copy source files
COPY src/backend/Lingual.SharedKernel/ src/backend/Lingual.SharedKernel/
COPY src/backend/Lingual.Persistence/ src/backend/Lingual.Persistence/
COPY src/backend/Lingual.Infrastructure/ src/backend/Lingual.Infrastructure/
COPY src/backend/Lingual.Modules.Identity.Contracts/ src/backend/Lingual.Modules.Identity.Contracts/
COPY src/backend/Lingual.Modules.Identity/ src/backend/Lingual.Modules.Identity/
COPY src/backend/Lingual.Modules.Learning.Contracts/ src/backend/Lingual.Modules.Learning.Contracts/
COPY src/backend/Lingual.Modules.Learning/ src/backend/Lingual.Modules.Learning/
COPY src/backend/Lingual.Modules.Gamification.Contracts/ src/backend/Lingual.Modules.Gamification.Contracts/
COPY src/backend/Lingual.Modules.Gamification/ src/backend/Lingual.Modules.Gamification/
COPY src/backend/Lingual.Modules.MezonBot.Contracts/ src/backend/Lingual.Modules.MezonBot.Contracts/
COPY src/backend/Lingual.Modules.MezonBot/ src/backend/Lingual.Modules.MezonBot/
COPY src/backend/Lingual.Modules.AITutor.Contracts/ src/backend/Lingual.Modules.AITutor.Contracts/
COPY src/backend/Lingual.Modules.AITutor/ src/backend/Lingual.Modules.AITutor/
COPY src/backend/Lingual.Api/ src/backend/Lingual.Api/

# Publish
RUN dotnet publish src/backend/Lingual.Api/Lingual.Api.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /out ./

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Lingual.Api.dll"]
