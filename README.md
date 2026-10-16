# Lock Code

Rust Oxide/Carbon plugin that shows admins the code, guest code and authorized players of a lock.
Rust-Oxide/Carbon-Plugin, das Admins den Code, Gastcode und die berechtigten Spieler eines Schlosses anzeigt.

**Author:** ThornFox · **Version:** 1.1.0 · **Framework:** Oxide/uMod, compatible with Carbon

---

## English

### Usage
Look at a door, box, locker, tool cupboard, vehicle or directly at a lock and type:

```
/lockcode
```

The output shows the lock owner, lock state, code, guest code, authorized players and guests.
Key locks show the key code. Code locks on modular cars are supported as well.

### Permission
| Permission | Description |
|---|---|
| `lockcode.use` | Allows the use of `/lockcode` |

```
oxide.grant group admin lockcode.use
```

Admins can use the command without the permission as long as `Admins can use without permission` is `true`.

### Configuration
```json
{
  "Config version": "1.1.0",
  "Max look distance (m)": 10.0,
  "Admins can use without permission": true,
  "Log usage to file": true,
  "Max listed players per list": 15
}
```

### Logging
Every query is written to `oxide/logs/LockCode/` (who queried whose lock and where). The code itself is never logged.

### Localization
English and German are included (`oxide/lang/en/LockCode.json`, `oxide/lang/de/LockCode.json`).

### Changelog
- **1.1.0**
  - Author set to ThornFox, bilingual description
  - All texts via the Lang API (en + de), code and comments in English
  - Configuration file with version migration (distance, admin bypass, logging, list length)
  - Support for code locks on modular cars
  - Rich text tags in player names are stripped, long player lists are shortened
- **1.0.1** – Initial version

---

## Deutsch

### Benutzung
Auf Tür, Kiste, Spind, Werkzeugschrank, Fahrzeug oder direkt auf das Schloss schauen und eingeben:

```
/lockcode
```

Angezeigt werden Besitzer, Schlosszustand, Code, Gastcode, berechtigte Spieler und Gäste.
Bei Schlüsselschlössern wird der Key-Code angezeigt. Codeschlösser an modularen Autos werden ebenfalls unterstützt.

### Berechtigung
| Berechtigung | Beschreibung |
|---|---|
| `lockcode.use` | Erlaubt die Nutzung von `/lockcode` |

```
oxide.grant group admin lockcode.use
```

Admins können den Befehl ohne Berechtigung nutzen, solange `Admins can use without permission` auf `true` steht.

### Konfiguration
Siehe Beispiel oben. Die Reichweite, der Admin-Bypass, das Logging und die maximale Länge der Namenslisten sind einstellbar. Alte Konfigurationen werden beim Laden automatisch migriert.

### Protokoll
Jede Abfrage wird in `oxide/logs/LockCode/` gespeichert (wer wessen Schloss wo abgefragt hat). Der Code selbst wird nie protokolliert.

### Sprachen
Englisch und Deutsch sind enthalten.

### Änderungen
- **1.1.0**
  - Autor auf ThornFox gesetzt, zweisprachige Beschreibung
  - Alle Texte über die Lang-API (en + de), Code und Kommentare auf Englisch
  - Konfigurationsdatei mit Versionsmigration (Reichweite, Admin-Bypass, Logging, Listenlänge)
  - Unterstützung für Codeschlösser an modularen Autos
  - Rich-Text-Tags in Spielernamen werden entfernt, lange Spielerlisten gekürzt
- **1.0.1** – Erste Version
