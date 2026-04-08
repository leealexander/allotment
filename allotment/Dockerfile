#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Runtime image - arm32v7
FROM --platform=linux/arm/v7 mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apt update && apt install tzdata -y && rm -rf /var/lib/apt/lists/*
RUN apt-get update && apt-get install -y procps && rm -rf /var/lib/apt/lists/*
ENV TZ="Europe/London"

# Build on native host platform for speed (cross-compile for arm32v7)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["allotment/allotment.csproj", "allotment/"]
RUN dotnet restore "allotment/allotment.csproj" -r linux-arm
COPY . .
WORKDIR "/src/allotment"
RUN dotnet publish "allotment.csproj" -c Release -r linux-arm --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "allotment.dll"]
#ENTRYPOINT ["tail", "-f", "/dev/null"]