FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["angeAuth.csproj", "./"]
RUN dotnet restore "angeAuth.csproj"
COPY . .
RUN dotnet publish "angeAuth.csproj" -c Release -o /app/publish

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5111
EXPOSE 5111
ENTRYPOINT ["dotnet", "angeAuth.dll"]
