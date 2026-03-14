# ADR-003: Documentation-as-Code mit XML-Docs und ADRs (M3)

**Status:** Akzeptiert  
**Datum:** 2025-03-01  
**Entscheider:** Core Banking Architecture Board

---

## Kontext

Dokumentation im Kernbanken-Umfeld ist typischerweise:
- Veraltet (Aktualitätsrating 4/5, aber minimales Volumen – Experte A, 2025)
- In separaten Systemen gepflegt (Confluence, Word-Dokumente)
- Für KI-Assistenzsysteme nicht zugänglich (kein Teil des Code-Repositories)

KI-Assistenzsysteme wie GitHub Copilot haben ausschliesslich Zugriff
auf den Code-Kontext (geöffnete Dateien, aktives Repository). Externe
Dokumentation ist unsichtbar.

Das Onboarding neuer Entwickler dauert im untersuchten System ca. 3 Monate
– ein direkter Indikator für mangelnde In-Code-Dokumentation.

## Entscheidung

Wir führen **Documentation-as-Code** ein:

1. **XML-Dokumentationskommentare** (`///`) auf allen public-Interfaces,
   -Klassen, -Records und -Methoden (C#-Standard, generiert HTML-Docs)
   
2. **Architecture Decision Records (ADRs)** für alle Architekturentscheide
   (dieses Dokument ist ein Beispiel). Format: Nygard-ADR-Template.
   Ablage: `docs/adr-NNN-kurztitel.md` im jeweiligen Context.

3. **README.md** pro Context mit:
   - Fachlicher Beschreibung des Contexts (1 Absatz)
   - Liste der Kernentitäten (Ubiquitous Language Glossar-Verweis)
   - Einstiegspunkt für KI-Assistenzsysteme (Welche Interfaces existieren?)

## Konsequenzen

**Positiv:**
- GitHub Copilot liest XML-Docs und ADRs als Teil des Repo-Kontexts
- Erster Copilot-Vorschlag verwendet korrekte Domänenbegriffe
- Onboarding-Zeit reduziert sich (Ziel: < 4 Wochen für Kernfunktionen)
- Architekturentscheide sind nachvollziehbar und auffindbar

**Negativ:**
- Schreibaufwand bei jeder neuen öffentlichen API
- Veraltete Kommentare können irreführend sein (Disziplin erforderlich)

## Enforcement

- CI-Gate: `dotnet build /p:TreatWarningsAsErrors=true` – fehlende
  XML-Docs auf public Members erzeugen Compiler-Warnung CS1591
- Code-Review-Checkliste: ADR vorhanden bei neuen Architekturentscheiden?
