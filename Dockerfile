FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/Messenger.WebApi/Messenger.WebApi.csproj", "src/Messenger.WebApi/"]
COPY ["src/Messenger.Application/Messenger.Application.csproj", "src/Messenger.Application/"]
COPY ["src/Messenger.Domain/Messenger.Domain.csproj", "src/Messenger.Domain/"]
COPY ["src/Messenger.Infrastructure/Messenger.Infrastructure.csproj", "src/Messenger.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "./src/Messenger.WebApi/Messenger.WebApi.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/src/Messenger.WebApi"

# Build the application
RUN dotnet build "./Messenger.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Messenger.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render dynamically assigns a PORT environment variable. 
# We tell ASP.NET Core to listen on this port.
ENV ASPNETCORE_HTTP_PORTS=
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Messenger.WebApi.dll"]
