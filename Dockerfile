#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Hotels.csproj", "."]
RUN dotnet restore "./Hotels.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Hotels.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hotels.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ADD Init /Init
ENTRYPOINT ["dotnet", "Hotels.dll"]