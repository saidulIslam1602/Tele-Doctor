# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj", "src/TeleDoctor.WebAPI/"]
COPY ["src/TeleDoctor.Core/TeleDoctor.Core.csproj", "src/TeleDoctor.Core/"]
COPY ["src/TeleDoctor.Application/TeleDoctor.Application.csproj", "src/TeleDoctor.Application/"]
COPY ["src/TeleDoctor.Infrastructure/TeleDoctor.Infrastructure.csproj", "src/TeleDoctor.Infrastructure/"]
COPY ["src/TeleDoctor.AI.Services/TeleDoctor.AI.Services.csproj", "src/TeleDoctor.AI.Services/"]
COPY ["src/TeleDoctor.Norwegian.Integration/TeleDoctor.Norwegian.Integration.csproj", "src/TeleDoctor.Norwegian.Integration/"]

RUN dotnet restore "src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/TeleDoctor.WebAPI"
RUN dotnet build "TeleDoctor.WebAPI.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "TeleDoctor.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install necessary packages for Norwegian locale support
RUN apt-get update && apt-get install -y \
    locales \
    && rm -rf /var/lib/apt/lists/*

# Generate Norwegian locale
RUN sed -i '/nb_NO.UTF-8/s/^# //g' /etc/locale.gen && \
    locale-gen

# Set environment variables for Norwegian locale
ENV LANG=nb_NO.UTF-8
ENV LANGUAGE=nb_NO:nb
ENV LC_ALL=nb_NO.UTF-8

# Create non-root user for security
RUN groupadd -r teledoctor && useradd -r -g teledoctor teledoctor

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R teledoctor:teledoctor /app

# Switch to non-root user
USER teledoctor

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "TeleDoctor.WebAPI.dll"]
