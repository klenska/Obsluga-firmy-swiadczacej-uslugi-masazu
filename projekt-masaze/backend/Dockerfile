# Wybieramy lekką wersję środowiska Node.js
FROM node:18-alpine

# Ustawiamy katalog roboczy wewnątrz kontenera
WORKDIR /app

# Kopiujemy najpierw pliki z listą zależności (package.json i package-lock.json)
COPY package*.json ./

# Instalujemy zależności
RUN npm install

# Kopiujemy resztę kodu aplikacji
COPY . .

# Informujemy, że aplikacja będzie działać na porcie 3000
EXPOSE 3000

# Komenda uruchamiająca serwer
CMD ["npm", "start"]
