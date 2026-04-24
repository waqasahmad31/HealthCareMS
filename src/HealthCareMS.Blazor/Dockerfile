FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "src/HealthCareMS.Blazor/HealthCareMS.Blazor.csproj"
RUN dotnet publish "src/HealthCareMS.Blazor/HealthCareMS.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html

RUN apk add --no-cache gettext

COPY deploy/nginx/healthcarems.blazor.conf /etc/nginx/conf.d/default.conf
COPY src/HealthCareMS.Blazor/docker/entrypoint.sh /entrypoint.sh
COPY src/HealthCareMS.Blazor/docker/appsettings.template.json /config/appsettings.template.json
COPY --from=build /app/publish/wwwroot ./

RUN chmod +x /entrypoint.sh

ENV API_BASE_URL=http://localhost:5270
ENTRYPOINT ["/entrypoint.sh"]

