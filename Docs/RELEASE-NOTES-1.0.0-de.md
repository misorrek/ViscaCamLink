# ViscaCamLink 1.0.0

ViscaCamLink 1.0.0 ist ein großes Update gegenüber 0.3.0: benannte Voreinstellungen in Gruppen, anpassbare Hotkeys, Maussteuerung, ein dunkles Design, automatische Updates und ein neuer Installer.

## Highlights

- **Mehr Voreinstellungen**: Voreinstellungen in Gruppen zu je 10, insgesamt bis zu 255 Kamera-Speicherplätze (0–254). Gruppen über eine Tab-Leiste wechseln, hinzufügen, entfernen und umbenennen.
- **Benannte Voreinstellungen**: Doppelklick auf den Namen einer Voreinstellung oder Gruppe zum Umbenennen (Enter bestätigt, Escape bricht ab). Wahl zwischen Numpad-Anordnung (Standard) und normaler Anordnung.
- **Eigene Hotkeys**: unter Einstellungen → Hotkeys eigene Tasten für Voreinstellungen, Bewegung, Zoom, Kameraprofile und Gruppen festlegen. Doppelte Belegungen werden erkannt; globale Hotkeys lassen sich ein- und ausschalten.
- **Schwenken/Neigen mit der Maus**: auf dem mittleren Schalter klicken und ziehen. Je weiter von der Mitte entfernt, desto schneller bewegt sich die Kamera.
- **Automatische Updates**: die App sucht beim Start nach neuen Versionen, zeigt die Änderungen, lädt das Update herunter und startet sich neu.
- **Designs**: hell, dunkel oder live passend zum Windows-App-Modus.

## Verbesserungen

- Verbindungsprüfung: der Status zeigt an, wenn die Kamera nicht erreichbar ist, und erholt sich automatisch, sobald sie wieder da ist.
- Fehlgeschlagene Speicherungen auf der Kamera werden korrekt gemeldet; der Verbindungsaufbau startet nach dem Anzeigen des Hauptfensters.
- Numpad-Hotkeys sind standardmäßig nur in der App aktiv; globale Hotkeys lassen sich unter Einstellungen einschalten. Eine gespeicherte Hotkey-Einstellung bleibt unverändert.
- Deutlicher sichtbarer aktiver Zustand der Seitenleisten-Schalter in beiden Designs.
- Voreinstellungen: Entfernen einer Gruppe fragt nach; der Schalter zum Hinzufügen erklärt, wenn das Maximum erreicht ist; der Tooltip des Speichern-Schalters passt sich während der Platzauswahl an; ein einmaliger Tipp erklärt das Umbenennen.
- Einstellungen: neuer Bereich Support mit Auswahl der Protokollstufe (wirkt sofort), „Protokollordner öffnen“, „Tipps wieder anzeigen“ und der App-Version.
- Kameraprofile: zeigt IP-Adresse und Port, markiert das aktive Profil, fragt vor dem Löschen nach, zeigt Eingabefehler als Text und unterstützt Enter/Escape beim Bearbeiten.
- Die App läuft jetzt auf .NET 10, bringt alles Nötige mit und schreibt für den Support täglich eine Protokolldatei.

## Update von 0.3.0

- 1.0.0 verwendet einen neuen Installer (`ViscaCamLink.App-win-Setup.exe`). Er installiert pro Benutzer, ohne Administratorrechte, nach `%LocalAppData%\ViscaCamLink.App\`. 0.3.0 bietet das Update automatisch an.
- Beim ersten Start werden Kamera-IP-Adresse und Port, Sichtbarkeit der Panels, Geschwindigkeiten und Sprache automatisch aus 0.3.0 übernommen.
- Ist die alte Version noch installiert, bietet die App einmalig die Deinstallation an (erfordert eine Administratorbestätigung). Alternativ später unter Windows-Einstellungen → Apps entfernen. Alte portable Ordner bleiben unberührt und können selbst gelöscht werden.
- Speicherorte:
  - Einstellungen, Voreinstellungen und Hotkeys: `%AppData%\ViscaCamLink\` (bleiben bei Update und Deinstallation erhalten)
  - Protokolle: `%LocalAppData%\ViscaCamLink.App\logs\` (14 Tage aufbewahrt)

## Voraussetzungen

- Windows 10 oder 11, x64
- Keine .NET-Installation nötig

## Bekannte Einschränkungen

- Ein Doppelklick auf einen Gruppen-Tab wechselt zuerst die Gruppe und startet dann das Umbenennen. Maximal 25 volle Gruppen plus eine 26. Gruppe mit 5 Voreinstellungen; viele Gruppen umbrechen in mehrere Tab-Zeilen.
- Bestätigungsdialoge verwenden das Standard-Meldungsfenster von Windows und folgen nicht dem Design. Der Umbenennen-Tipp erscheint nur im Hauptfenster.
- Escape kann nicht als Hotkey belegt werden. Nur die Voreinstellungen 0–9 der aktiven Gruppe können Hotkeys haben.
- Die Verbindungsprüfung sendet alle 5 Sekunden eine kleine Abfrage; ein kurzer Ausfall zeigt kurz einen Verbindungsfehler an, der sich dann von selbst auflöst.
- Diagonale Bewegung mit Höchstgeschwindigkeit ist nicht exakt 45° (Schwenk- und Neige-Höchstgeschwindigkeit unterscheiden sich, wie bei echten Kameras).
- Die Änderungen im Update-Dialog sind eine GitHub-Seite und folgen nicht dem Design.
- Unerwartete Fehler werden protokolliert, die App schließt sich aber ohne Fehlerdialog.
- Iris- und Fokussteuerung sind nicht Teil dieses Releases.
- Die App ist noch nicht code-signiert, daher kann Windows SmartScreen bei der ersten Installation eine Warnung zeigen.
