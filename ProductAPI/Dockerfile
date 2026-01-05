# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ProductAPI.csproj", "./"]
RUN dotnet restore "ProductAPI.csproj"
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Use the lighter ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductAPI.dll"]