# ADR-001: Einsatz von Bounded Contexts zur Boundary-Schärfung (M1)

**Status:** Akzeptiert  
**Datum:** 2025-03-01  
**Entscheider:** Core Banking Architecture Board

---

## Kontext

Das bestehende Kernbanken-System weist eine stark verwobene Codebasis auf:
Geschäftslogik für Kontoführung, Zahlungsverarbeitung und Kundenverwaltung
ist in gemeinsamen Klassen und Modulen vermischt. Dies führt zu:

- Hoher kognitiver Last bei der Einarbeitung neuer Entwickler
- Mangelnder Kontextklarheit für KI-Assistenzsysteme (GitHub Copilot)
- Schwieriger Testbarkeit durch implizite Abhängigkeiten
- Fehleranfälligen Änderungen durch unklare Verantwortungsgrenzen

## Entscheidung

Wir führen **Bounded Contexts** nach Domain-Driven Design (Evans, 2003) ein.
Jeder Context kapselt:
1. Eigene Domänenmodelle (keine Wiederverwendung über Context-Grenzen)
2. Ein explizites Service-Interface (kein direkter Repository-Zugriff von aussen)
3. Eine eigene Ubiquitous Language (Begriffe aus dem Glossar der Bankfachlichkeit)

Die drei initialen Contexts sind: **Account**, **Payment**, **Customer**.

## Konsequenzen

**Positiv:**
- KI-Assistenzsysteme erhalten klaren Kontext: Datei-Pfade, Typ-Namen und
  XML-Kommentare signalisieren eindeutig den fachlichen Bereich
- Neue Funktionen können im richtigen Context platziert werden ohne
  Analyse der gesamten Codebasis
- Unit-Tests pro Context sind isoliert möglich

**Negativ:**
- Initialer Aufwand für Refactoring und Migration
- Redundante Modelle (z.B. IBAN kommt in Account und Payment vor)
  → Akzeptabel: Context-eigene Modelle sind ausdrücklich erwünscht (DDD-Prinzip)

## Alternativen verworfen

- **Schichtenarchitektur (Layer):** Trennt nach technischen Schichten,
  nicht nach fachlichen Grenzen → KI-Kontext bleibt unklar
- **Microservices sofort:** Zu hoher Betriebsaufwand für aktuelle Teamgrösse
