# ── Stage 1: build ────────────────────────────────────────────────────────────
# node:22-slim (Debian-based) avoids Alpine-specific npm "Exit handler" bugs
FROM node:22-slim AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ── Stage 2: serve with Nginx ─────────────────────────────────────────────────
FROM nginx:1.27-alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html

# Nginx config: proxy /api requests to the backend service
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
