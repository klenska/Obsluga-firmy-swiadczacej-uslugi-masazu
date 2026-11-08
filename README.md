# Masu Masu SPA – Projekt A

Frontend demonstracyjny systemu rezerwacji masażu w HTML, CSS i JavaScript.

## Publikacja w istniejącym repozytorium

Do katalogu głównego repozytorium obok ISTNIEJĄCEGO pliku `Dockerfile` skopiuj:
- `index.html`
- `package.json`
- `build.js`

**Nie zastępuj ani nie usuwaj istniejącego Dockerfile.**

Foldery `MATERIALY_MASAZ-25` i `zrzuty_ekranu` zawierają materiały dokumentacyjne. Możesz je załączyć do Jira/Confluence. Nie są wymagane do uruchomienia strony.

## Uruchomienie

Otwórz `index.html` w przeglądarce, aby zobaczyć wersję demonstracyjną.

Aby zbudować pliki dla istniejącego Dockerfile:

```bash
npm install
npm run build
```

Powstaje `build/index.html`, a Dockerfile kopiuje `/app/build` do serwera Nginx.

## Ograniczenia

Rezerwacje są zapisywane w lokalnej pamięci przeglądarki (localStorage). Integracje API są symulacjami i nie łączą się z systemami B, C ani D. Zapobieganie konfliktom między osobnymi użytkownikami wymaga backendu. Wyniki testów są dowodami wcześniejszej weryfikacji prototypu, a końcowy odbiór należy do testera zespołu.
