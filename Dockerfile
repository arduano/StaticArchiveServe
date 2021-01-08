FROM mcr.microsoft.com/dotnet/sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out
RUN rm -rf /app/out/files

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet
WORKDIR /app
COPY --from=build-env /app/out .

# RUN apt update && apt install curl -y

ENTRYPOINT ["dotnet", "StaticArchiveServe.dll"]