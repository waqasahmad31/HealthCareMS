#!/bin/sh
set -eu

envsubst '${API_BASE_URL}' < /config/appsettings.template.json > /usr/share/nginx/html/appsettings.json

exec nginx -g 'daemon off;'

