# ============================================
# Stage 1: BUILD
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PolicyBooking.sln", "./"]
COPY ["src/Services/PolicyService/PolicyService.Worker/PolicyService.Worker.csproj", "src/Services/PolicyService/PolicyService.Worker/"]
COPY ["src/Services/PolicyService/PolicyService.Application/PolicyService.Application.csproj", "src/Services/PolicyService/PolicyService.Application/"]
COPY ["src/Services/PolicyService/PolicyService.Domain/PolicyService.Domain.csproj", "src/Services/PolicyService/PolicyService.Domain/"]
COPY ["src/Services/PolicyService/PolicyService.Infrastructure/PolicyService.Infrastructure.csproj", "src/Services/PolicyService/PolicyService.Infrastructure/"]

# Restore packages
RUN dotnet restore "src/Services/PolicyService/PolicyService.Worker/PolicyService.Worker.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/src/Services/PolicyService/PolicyService.Worker"
RUN dotnet build "PolicyService.Worker.csproj" -c Release -o /app/build

# ============================================
# Stage 2: PUBLISH
# ============================================
FROM build AS publish
RUN dotnet publish "PolicyService.Worker.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ============================================
# Stage 3: RUNTIME (Final Image)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r workeruser && useradd -r -g workeruser workeruser
RUN chown -R workeruser:workeruser /app
USER workeruser

# Environment variables
ENV DOTNET_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD pgrep -f "PolicyService.Worker" > /dev/null || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PolicyService.Worker.dll"]