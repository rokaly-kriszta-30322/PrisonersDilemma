# Stage 1: Build React frontend
FROM node:18 AS frontend-build
WORKDIR /app/frontend

COPY frontend/package*.json ./
RUN npm install

COPY frontend/ ./
RUN npm run build

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS backend-build
WORKDIR /app/backend

COPY backend/*.csproj ./
RUN dotnet restore

COPY backend/ ./
# Copy React build output into backend's wwwroot (for static files)
COPY --from=frontend-build /app/frontend/build ./wwwroot

RUN dotnet publish -c Release -o out

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=backend-build /app/backend/out .

EXPOSE 80

ENTRYPOINT ["dotnet", "backend.dll"]