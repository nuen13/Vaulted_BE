# One image for Cloud Run: the React frontend is built and served by the .NET API from wwwroot,
# so the browser talks to one origin (/api on the same host) and no CORS entry is needed.

# --- Frontend build ---
FROM node:22-slim AS frontend
WORKDIR /fe
COPY Vaulted_FE/package.json Vaulted_FE/package-lock.json ./
RUN npm ci
COPY Vaulted_FE/ ./
# Vite bakes these into the bundle at build time; both are public values, not secrets.
ARG VITE_GOOGLE_CLIENT_ID
ENV VITE_API_BASE_URL=/api \
    VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID
RUN npm run build

# --- API build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend
WORKDIR /src
COPY Vaulted.csproj ./
RUN dotnet restore Vaulted.csproj
COPY . .
# The frontend sources aren't part of the API; keep their files out of the publish output.
RUN rm -rf Vaulted_FE \
 && dotnet publish Vaulted.csproj -c Release -o /app --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /fe/dist ./wwwroot
# Cloud Run ends HTTPS and forwards plain HTTP on 8080 (the image's default port);
# trusting X-Forwarded-Proto stops UseHttpsRedirection from looping.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
USER $APP_UID
ENTRYPOINT ["dotnet", "Vaulted.dll"]
