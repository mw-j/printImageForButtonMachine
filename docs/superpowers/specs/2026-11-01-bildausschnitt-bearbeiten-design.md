# Design: Bildausschnitt automatisch ausrichten und von Hand korrigieren

Stand: 2026-11-01

## Anlass

Beim ersten realen Einsatz (Fotobox mit Canon EOS 77D und EOS Utility, Personen auf einem Stuhl: kleine Kinder,
Erwachsene mit Kind auf dem Schoß, bis zu drei Personen gleichzeitig) sind zwei Dinge aufgefallen:

1. **Keine Rückmeldung beim Drucken.** Nach „Jetzt drucken“ mit voller Seite vergeht eine spürbare Zeit, ohne dass das
   Programm anzeigt, dass es arbeitet. Der Anwender weiß nicht, ob der Klick angekommen ist.
2. **Bildausschnitt passt oft nicht.** Das Programm schneidet immer einen Kreis aus der Bildmitte (Anteil
   „Größe des Bildausschnitts (%)“ der kürzeren Kante). Weil die Gesichter je nach Personen unterschiedlich lagen, musste
   häufig Kamera-Neigung und Zoom nachgestellt, mehrfach fotografiert (3–5 Fotos, bis eins mittig war) oder die
   Prozent-Einstellung verändert werden.

Ziel: Ein großzügiges Foto aufnehmen, das Programm schlägt den Ausschnitt anhand der Gesichter vor, und der Anwender
kann ihn bei Bedarf schnell von Hand zurechtschieben und zoomen.

## Ziele

- Neue Fotos bekommen automatisch einen Ausschnitt, der alle erkannten Gesichter im später sichtbaren Bereich des Buttons
  zeigt.
- Jeder Kreis in der Vorschau lässt sich per Doppelklick in einem Bearbeiten-Fenster verschieben und zoomen.
- Korrekturen bleiben pro Foto erhalten (auch nach Programmneustart) und werden beim Druck verwendet.
- Während eine Seite gedruckt wird, ist klar sichtbar, dass das Programm arbeitet.
- Läuft flüssig genug auf einem älteren Notebook (das Einsatzgerät).

## Nicht-Ziele

- **Keine Kamerasteuerung im Programm.** Fotos kommen weiterhin über ein beliebiges Fernsteuerungsprogramm (z. B.
  EOS Utility) in den überwachten Ordner. Das Programm bleibt kamera- und herstellerunabhängig. Eine eingebaute
  Steuerung für alle Hersteller (PTP-Erweiterungen, Hersteller-SDKs) wäre ein eigenes, großes Vorhaben.
- Keine Augen-/Landmarkenerkennung, keine Bildverbesserung (Helligkeit, Rote Augen o. Ä.).
- Keine Änderung am Seitenlayout (Abstände, Anordnung der Kreise auf A4).

## Anti-Ziele (wäre ein Fehlschlag, auch wenn es technisch funktioniert)

- Die Vorschau oder die Ordnerüberwachung wird auf dem alten Notebook spürbar träge.
- Ein Button bekommt einen weißen/leeren Rand, weil zu weit herausgezoomt wurde.
- Ein Foto wird gedruckt, während der Anwender es gerade bearbeitet.
- Der Hilfskreis oder andere Bildschirm-Hilfslinien landen auf dem Ausdruck.
- Die exe wird durch große Zusatzbibliotheken (OpenCV o. Ä.) deutlich größer.

## Rahmenbedingungen

- WinForms, .NET 10, Windows 10/11; Auslieferung als eigenständige Single-File-exe.
- Fotos der 77D: JPEG mit 24 MP (6000 × 4000). Dekodieren kostet schon heute einige hundert Millisekunden pro Bild und
  ist vermutlich auch der Hauptgrund für die Druckverzögerung.
- Druck mit 300 dpi in Originalgröße (bestehend, siehe `ImageProcessor`).
- Überwachter Ordner wird alle 2 Sekunden gelesen; es zählen nur `*.jpg`.
- Typischer Arbeitsablauf: einige Fotos machen, Vorschau prüfen, dann meist manuell „Jetzt drucken“.

## Begriffe

- **Durchmesser:** bestehende Einstellung „Durchmesser der Bilder (cm)“, der gedruckte Kreis (z. B. 52 mm).
- **Sichtbarer Bereich:** Kreis mit 4 mm kleinerem Radius (also 8 mm kleinerem Durchmesser, z. B. 44 mm). Der äußere Ring
  wird beim Pressen um den Button umgeschlagen und ist nicht sichtbar.
- **Ausschnitt:** der Teil des Fotos, der im gedruckten Kreis landet; beschrieben durch Mittelpunkt und Größe.

## Festgelegtes Verhalten

### 1. Rückmeldung beim Drucken

- Während eine Seite vorbereitet und an den Drucker geschickt wird (manuell oder automatisch), sind alle Knöpfe gesperrt
  („Jetzt drucken“ zeigt dabei „Seite wird gedruckt …“). Die Vorschau wird halbtransparent überdeckt mit Ladekreis,
  „Seite wird gedruckt …“, einem laufenden Fortschrittsbalken und z. B. „5 Bilder werden aufbereitet“; zusätzlich
  Wartecursor über dem Fenster.
- Nach Abschluss verschwindet der Hinweis; Fehler werden wie bisher per Meldung angezeigt.

### 2. Automatische Ausrichtung

- Taucht ein neues Foto im überwachten Ordner auf, wird **einmalig** eine Gesichtserkennung ausgeführt und daraus ein
  Ausschnitt berechnet. Das Ergebnis wird gespeichert und danach nur noch verwendet (keine Erkennung bei jedem
  Vorschau-Durchlauf).
- Gesichtserkennung über die in Windows eingebaute API `Windows.Media.FaceAnalysis.FaceDetector` (offline, keine
  Zusatzbibliothek). Die Erkennung läuft auf einer verkleinerten Kopie des Fotos (Größenordnung 1.500 px lange Kante).
- Berechnung (Entscheidung des Nutzers): Die **Größe** des Ausschnitts kommt weiterhin aus der Prozent-Einstellung
  „Größe des Bildausschnitts (%)“, nur der **Mittelpunkt** wandert auf die Gesichter. Dazu wird ein Rahmen um alle
  erkannten Gesichter gelegt, um einen Zuschlag erweitert (Haare, Kinn), und der Ausschnitt darauf zentriert. Passt dieser
  Rahmen bei der eingestellten Größe nicht in den **sichtbaren Bereich**, wird so weit herausgezoomt, bis er passt.
  Hineingezoomt wird automatisch nie; ein einzelnes kleines Gesicht wird also nicht formatfüllend.
- Wird kein Gesicht gefunden: mittiger Ausschnitt mit der Prozent-Einstellung wie bisher.
- Automatische Ausschnitte folgen Änderungen der Einstellungen: Ändert der Anwender Prozent oder Durchmesser, werden sie
  aus den gespeicherten Gesichtsdaten neu berechnet (ohne erneute Erkennung). Von Hand bearbeitete Ausschnitte bleiben
  unverändert.
- Fotos im Hochformat: Die EXIF-Ausrichtung wird überall ausgewertet (Erkennung, Vorschau, Bearbeiten, Druck), damit
  hochkant gespeicherte Fotos richtig herum verarbeitet werden. Bisher wird sie ignoriert. *(Entscheidung Claude)*
- Der Ausschnitt unterliegt denselben Grenzen wie die Handbearbeitung (siehe unten): Der äußere Kreis muss vollständig mit
  Foto gefüllt sein. Passen die Gesichter dann nicht ganz in den sichtbaren Bereich (z. B. drei Personen nah am Bildrand),
  wird das hingenommen; der Anwender kann von Hand korrigieren.

### 3. Bearbeiten-Fenster

- Öffnet sich per Doppelklick auf einen Kreis in der Vorschau. Beim Überfahren mit der Maus bekommt der Kreis einen
  blauen Rahmen, einen Handcursor und den Tooltip „Doppelklick: Ausschnitt bearbeiten“. Unter der Vorschau steht
  zusätzlich „Doppelklick auf ein Bild zum Bearbeiten“; der Einleitungstext oben im Hauptfenster erwähnt die
  automatische Ausrichtung und den Doppelklick.
- Von Hand korrigierte Kreise tragen in der Vorschau ein kleines blaues Stift-Symbol unten rechts.
- Fenstertitel „Bildausschnitt bearbeiten – <Dateiname>“; unten links Hinweis „Ziehen = verschieben · Mausrad = zoomen ·
  N Gesichter erkannt“.
- Fester Kreis in der Mitte, das Foto liegt darunter. Außerhalb des Kreises bleibt das Foto abgedunkelt sichtbar.
- Zwei Kreislinien: außen weiß der Druckrand (Durchmesser), innen gelb gestrichelt der sichtbare Bereich (Radius − 4 mm),
  jeweils mit kleiner Beschriftung (z. B. „Druckrand 52 mm“, „sichtbar 44 mm“). In der kleinen Vorschau des Hauptfensters
  wird kein Hilfskreis gezeigt.
- Verschieben mit gedrückter Maus, Zoomen mit dem Mausrad an der Mausposition.
- Knöpfe: „Automatisch“ (zurück auf den Vorschlag der Gesichtserkennung bzw. den mittigen Fallback), „OK“, „Abbrechen“.
  Enter = OK, Esc = Abbrechen.
- Das Foto wird in ausreichender Auflösung angezeigt, um bei starkem Zoom die Schärfe beurteilen zu können.
- Herauszoomen und Verschieben nur so weit, dass der äußere Kreis vollständig mit Foto gefüllt bleibt.
- Hoch- und Querformat verhalten sich gleich, nur mit anderem Spielraum.
- Der Hilfskreis erscheint nur am Bildschirm, nie auf dem Ausdruck.

### 4. Speicherung und Druck

- Pro Foto wird eine kleine Begleitdatei neben dem Foto gespeichert: die erkannten Gesichter und, falls von Hand
  bearbeitet, der manuelle Ausschnitt. Sie bleibt über einen Programmneustart erhalten und wird beim Archivieren zusammen
  mit dem Foto in den Ausgabeordner verschoben. Fotos ohne Begleitdatei (z. B. aus älteren Versionen) werden beim ersten
  Sehen erkannt.
- Vorschau und Druck verwenden den gespeicherten Ausschnitt.
- Der automatische Druck bei voller Seite bleibt erhalten, wartet aber, solange ein Bearbeiten-Fenster offen ist.

## Dem Umsetzer überlassen

- Format und Name der Begleitdatei (darf nicht auf `.jpg` enden, damit sie nicht als Foto zählt). Koordinaten relativ
  zum richtig ausgerichteten Foto speichern, damit sie von Durchmesser und Auflösung unabhängig sind.
- Höhe des Zuschlags um die Gesichter; Feinschliff anhand der Testfotos.
- Wie weit man im Bearbeiten-Fenster hineinzoomen darf.
- Ob der automatische Druck auch wartet, bis die Erkennung aller Fotos der Seite abgeschlossen ist (sinnvoll).
- Einbindung der WinRT-API (z. B. Ziel-Framework `net10.0-windows10.0.19041.0`), sofern die Single-File-Auslieferung
  weiter funktioniert.
- Erkennung von Kreisen per Klickposition in der Vorschau (`PictureBox` mit `SizeMode.Zoom`).
- Ob und wie sich die Druckverzögerung zusätzlich verkürzen lässt; Pflicht ist nur die sichtbare Rückmeldung.

## Nachtrag 2026-11-01: Regel aus Handkorrekturen, Prozent-Einstellung entfällt

Nach einem Test hat der Nutzer fünf Fotos von Hand korrigiert (Button 5,8 cm). Muster: Er setzt den Rahmen der
(unerweiterten) Gesichter mittig, zoomt bei Einzelpersonen deutlich hinein und schneidet Gruppen enger als die bisherige
Automatik. Entscheidung des Nutzers: Die automatische Positionierung folgt diesem Muster, die Einstellung
„Größe des Bildausschnitts (%)“ entfällt. Damit ersetzt dieser Nachtrag die Berechnung in Abschnitt 2:

- Mitte = Mitte des Rahmens um alle erkannten Gesichter, ohne Zuschlag.
- Größe = der größere von zwei Werten, bezogen auf den sichtbaren Kreis: Das größte Gesicht ist 61 % so hoch wie der
  sichtbare Durchmesser; die halbe Diagonale des Gesichtsrahmens erreicht 93 % des sichtbaren Radius. Hineinzoomen ist
  damit erlaubt.
- Ohne Gesicht: mittig über die kürzere Bildkante.
- Abweichung zu den fünf Handkorrekturen: Größe meist unter 2 % (einmal 8 %), Mitte unter 4,2 % des Kreisdurchmessers.

## Reihenfolge und Vorab-Test *(Entscheidung Claude)*

Die Qualität der Windows-Gesichtserkennung bei den realen Motiven (Kinder, Halbprofil, mehrere Personen) und die
Laufzeit auf dem Einsatz-Notebook sind noch nicht gemessen. Deshalb zuerst ein Wegwerf-Test mit den Fotos vom ersten
Einsatz (`test-bilder/`, liegt lokal und wird nicht committet): Er zeichnet Gesichter und vorgeschlagene Kreise ein und
gibt die Zeiten aus, lauffähig auch auf dem Notebook.

- Druck-Rückmeldung, Bearbeiten-Fenster und Speicherung werden in jedem Fall umgesetzt.
- Ist die Erkennung zu schwach oder zu langsam, entfällt nur der automatische Vorschlag; Ausgangspunkt bleibt dann der
  mittige Ausschnitt, und eine andere Erkennung wäre ein eigenes Thema.

**Ergebnis des Tests (2026-11-01, Entwicklungs-PC):** Bestanden. Auf allen 10 Testfotos (3984 × 2656, 1–3 Personen,
auch Kleinkind auf dem Stuhl) wurden alle Gesichter gefunden, ohne Fehltreffer. Zeit pro Foto rund 100 ms (Laden und
Verkleinern auf 1.500 px Kante in Gray8 ca. 80 ms, `DetectFacesAsync` ca. 20 ms). Bewährte Parameter: Zuschlag um den
Gesichtsrahmen seitlich 15 %, oben 40 %, unten 15 %; Ausschnitt mittig auf diesen erweiterten Rahmen; Passprüfung über
die halbe Diagonale des Rahmens gegen den Radius des sichtbaren Bereichs. Die Bildgestaltung (Gesicht in der Kreismitte)
hat der Nutzer so abgenommen. Messung auf dem Einsatz-Notebook steht noch aus (Test-exe liegt vor).
Zu prüfen bei der Umsetzung: Größenzuwachs der exe durch die Windows-SDK-Projektion (Ziel-Framework mit
Windows-Versionsnummer).
