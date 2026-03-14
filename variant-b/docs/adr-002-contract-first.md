# ADR-002: Contract-First API-Design mit OpenAPI 3.1 (M2)

**Status:** Akzeptiert  
**Datum:** 2025-03-01  
**Entscheider:** Core Banking Architecture Board

---

## Kontext

Neue und bestehende Schnittstellen wurden bisher implizit definiert:
Keine formale API-Spezifikation existiert vor der Implementierung.
Änderungen an Schnittstellen sind nur durch Code-Lektüre erkennbar.
KI-Assistenzsysteme erhalten dadurch keinen strukturierten Kontext über
erwartete Inputs, Outputs und Fehlerszenarien.

Laut Experteninterview (Experte A, 2025) ist das Fehlen von
Contract-First-Artefakten der grösste Einzelhebel für bessere
KI-Unterstützung im untersuchten System.

## Entscheidung

Für jeden Bounded Context wird **zuerst eine OpenAPI 3.1 Spezifikation**
geschrieben, bevor Implementierungscode entsteht ("Contract-First").

Regeln:
1. OpenAPI-Dateien liegen unter `contracts/` im jeweiligen Context
2. Jeder `operationId` entspricht einer Methode im zugehörigen Interface
3. `description`-Felder verwenden Domänensprache (Ubiquitous Language)
4. Fehler-Codes sind als Enum in `ErrorResponse.errorCode` dokumentiert
5. Die OpenAPI-Dateien werden in der CI-Pipeline auf Gültigkeit geprüft

## Konsequenzen

**Positiv:**
- GitHub Copilot kann OpenAPI-Specs als Kontext nutzen und schlägt
  direkt korrekte Methodensignaturen, Parameter und Fehlerbehandlung vor
- Consumer-Teams können gegen die Spec entwickeln (Consumer-Driven Contracts)
- Breaking Changes werden durch Spec-Diff sichtbar (CI-Gate)

**Negativ:**
- Disziplin erforderlich: Spec muss bei Implementierungsänderungen
  synchron gehalten werden
- Initiales Schreiben der Specs kostet Zeit

## Werkzeuge

- **Validator:** `spectral lint` (Stoplight) in CI
- **Code-Generator:** `NSwag` für C#-Client-Stubs aus der Spec
- **Dokumentation:** Swagger UI automatisch aus der Spec generiert
