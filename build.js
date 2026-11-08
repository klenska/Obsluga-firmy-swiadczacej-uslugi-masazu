const fs = require('node:fs');
const path = require('node:path');

const projectDir = __dirname;
const source = path.join(projectDir, 'index.html');
const outputDir = path.join(projectDir, 'build');
const destination = path.join(outputDir, 'index.html');

if (!fs.existsSync(source)) {
  console.error('Błąd: nie znaleziono pliku index.html.');
  process.exit(1);
}

fs.mkdirSync(outputDir, { recursive: true });
fs.copyFileSync(source, destination);
console.log(`Zbudowano Masu Masu SPA: ${destination}`);
