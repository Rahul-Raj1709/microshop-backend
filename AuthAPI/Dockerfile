# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# FIX: Copy the AuthAPI project file, not ConsumerWorker
COPY ["AuthAPI.csproj", "./"]
RUN dotnet restore "AuthAPI.csproj"

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Use the lighter runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# This matches the project you just built
ENTRYPOINT ["dotnet", "AuthAPI.dll"]