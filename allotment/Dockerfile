#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#-bullseye-slim-arm32v7
ARG IMAGE_TAG_APPEND= 
FROM mcr.microsoft.com/dotnet/aspnet:6.0$IMAGE_TAG_APPEND AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apt update && apt install tzdata -y
ENV TZ="Europe/London"

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["allotment/allotment.csproj", "allotment/"]
RUN dotnet restore "allotment/allotment.csproj"
COPY . .
WORKDIR "/src/allotment"
RUN dotnet build "allotment.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "allotment.csproj" -c Release -o /app/publish
RUN apt update
RUN apt-get install -y procps && rm -rf /var/lib/apt/lists/*
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "allotment.dll"]
#ENTRYPOINT ["tail", "-f", "/dev/null"]