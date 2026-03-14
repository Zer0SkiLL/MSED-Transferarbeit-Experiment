#!/usr/bin/env python3
"""
q3-count.py – Zählt Q3-Domänenbegriffe in einem Copilot-Vorschlag.

Verwendung:
    python3 q3-count.py <datei>        # Datei als Input
    pbpaste | python3 q3-count.py -    # Clipboard als Input (macOS)
"""

import sys
import re

TERMS = {
    "iban":             r"\biban\b",
    "Girokonto/Sparkonto": r"\b(girokonto|sparkonto|festgeldkonto)\b",
    "BlockReason":      r"\bblock_?reason\b",
    "RemittanceInfo":   r"\b(remittanceinfo|verwendungszweck)\b",
    "AccountStatus":    r"\baccount_?status\b",
    "IncomingPayment":  r"\bincoming_?payment\b",
    "AuditEntry/Log":   r"\baudit_?(entry|log|eintrag)\b",
    "InitiatedBy":      r"\binitiated_?by\b",
    "creditor/debtor":  r"\b(creditor|debtor|glaeubiger|schuldner)\b",
    "NegativeBalance":  r"\bnegative_?balance\b",
}

def count(text: str) -> None:
    text_lower = text.lower()
    found = []
    not_found = []
    for label, pattern in TERMS.items():
        if re.search(pattern, text_lower):
            found.append(label)
        else:
            not_found.append(label)

    print(f"\n{'='*45}")
    print(f"  Q3-Ergebnis: {len(found)}/10 Domänenbegriffe")
    print(f"{'='*45}")
    if found:
        print(f"  Gefunden ({len(found)}):")
        for t in found:
            print(f"    [x] {t}")
    if not_found:
        print(f"  Nicht gefunden ({len(not_found)}):")
        for t in not_found:
            print(f"    [ ] {t}")
    print(f"{'='*45}\n")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Verwendung: python3 q3-count.py <datei>  ODER  pbpaste | python3 q3-count.py -")
        sys.exit(1)

    if sys.argv[1] == "-":
        text = sys.stdin.read()
    else:
        with open(sys.argv[1], "r", encoding="utf-8") as f:
            text = f.read()

    count(text)
