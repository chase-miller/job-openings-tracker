#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["OpeningsTracker/OpeningsTracker.Runners.BackgroundJob.csproj", "OpeningsTracker/"]
COPY ["LeverJobPostingSource/OpeningsTracker.JobPostingSources.Lever.csproj", "LeverJobPostingSource/"]
COPY ["Core/OpeningsTracker.Core.csproj", "Core/"]
COPY ["OpeningsTracker.DataStores.JsonFile/OpeningsTracker.DataStores.JsonFile.csproj", "OpeningsTracker.DataStores.JsonFile/"]
COPY ["EmailNotifier/OpeningsTracker.Notifiers.EmailNotifier.csproj", "EmailNotifier/"]
RUN dotnet restore "OpeningsTracker/OpeningsTracker.Runners.BackgroundJob.csproj"
COPY . .
WORKDIR "/src/OpeningsTracker"
RUN dotnet build "OpeningsTracker.Runners.BackgroundJob.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpeningsTracker.Runners.BackgroundJob.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpeningsTracker.Runners.BackgroundJob.dll"]