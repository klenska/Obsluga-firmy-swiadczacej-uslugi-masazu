\# ETAP 1: Budowanie aplikacji

FROM node:18-alpine AS build

WORKDIR /app

COPY package\*.json ./

RUN npm install

COPY . .

\# Komenda budująca wersję produkcyjną (zależnie od frameworka, np. w React to często npm run build)

RUN npm run build 



\# ETAP 2: Serwowanie gotowych plików

FROM nginx:alpine

\# Kopiujemy zbudowane pliki z poprzedniego etapu do folderu serwera Nginx

\# Uwaga: folder "/app/build" może się nazywać "/app/dist" (np. przy użyciu Vite) - trzeba to zweryfikować z zespołem

COPY --from=build /app/build /usr/share/nginx/html



EXPOSE 80

CMD \["nginx", "-g", "daemon off;"]

