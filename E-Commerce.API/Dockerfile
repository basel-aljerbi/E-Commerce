FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["E-Commerce.API.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

RUN mkdir -p /app/wwwroot/images

COPY --from=build /out .

ENTRYPOINT ["dotnet", "E-Commerce.API.dll", "--migrate"]
