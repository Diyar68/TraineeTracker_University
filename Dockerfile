FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
RUN useradd -u 5678 -m -s /bin/bash appuser
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TraineeTracker/TraineeTracker.csproj", "TraineeTracker/"]
RUN dotnet restore "TraineeTracker/TraineeTracker.csproj"
COPY ["TraineeTracker/", "TraineeTracker/"]
WORKDIR /src/TraineeTracker
RUN dotnet publish "TraineeTracker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "TraineeTracker.dll"]