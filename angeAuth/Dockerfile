FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el archivo del proyecto primero (para aprovechar la caché)
COPY ["angeAuth.csproj", "./"]
RUN dotnet restore "angeAuth.csproj"

# Copiar todo el resto del código
COPY . .

# Publicar
RUN dotnet publish "angeAuth.csproj" -c Release -o /app/publish

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5111
EXPOSE 5111
ENTRYPOINT ["dotnet", "angeAuth.dll"]
