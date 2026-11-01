# Bildausschnitt automatisch ausrichten und bearbeiten – Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Neue Fotos werden automatisch auf die Gesichter ausgerichtet, jeder Kreis lässt sich per Doppelklick in einem Bearbeiten-Fenster verschieben/zoomen, und während des Druckens zeigt das Programm deutlich, dass es arbeitet.

**Architecture:** Reine Rechenlogik (Ausschnitt, Seitenlayout, Begleitdatei, EXIF) in eigenen, UI-freien Klassen mit xUnit-Tests. Die Gesichtserkennung nutzt `Windows.Media.FaceAnalysis.FaceDetector` und schreibt das Ergebnis einmalig je Foto in eine Begleitdatei `<name>.button.json`. `ImageProcessor` liest daraus beim Rendern den Ausschnitt; `Form1` bekommt Hover/Doppelklick auf der Vorschau, eine Druck-Überlagerung und öffnet das neue `AusschnittForm`.

**Tech Stack:** C#, WinForms, .NET 10 (`net10.0-windows10.0.19041.0`), System.Drawing (GDI+), WinRT `FaceDetector`, System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-11-01-bildausschnitt-bearbeiten-design.md`

## Global Constraints

- Ziel-Framework beider Projekte: `net10.0-windows10.0.19041.0`; das Hauptprojekt bekommt **keine** NuGet-Pakete.
- Single-File-Publish muss weiter funktionieren: `dotnet publish PrintImageForButtonMachine -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`.
- Sichtbarer Bereich: Radius 4 mm kleiner als der Druckkreis, also Innendurchmesser = Durchmesser − 0,8 cm.
- Erkennung auf verkleinerter Kopie mit 1.500 px langer Kante, Pixelformat Gray8.
- Zuschlag um jeden Gesichtsrahmen: seitlich 15 %, oben 40 %, unten 15 % der Gesichtsbreite bzw. -höhe.
- Automatisch wird nie hineingezoomt; nur herausgezoomt, bis der erweiterte Gesichtsrahmen (halbe Diagonale) in den sichtbaren Bereich passt, höchstens bis zur kürzeren Bildkante.
- Der Ausschnitt ist immer ein Quadrat vollständig innerhalb des Fotos (kein weißer Rand), Kantenlänge 10 %–100 % der kürzeren Bildkante.
- Begleitdatei: `Path.ChangeExtension(foto, ".button.json")`; Koordinaten relativ zum richtig ausgerichteten Foto.
- Hilfskreis, Hover-Rahmen und Stift-Symbol erscheinen nie auf dem Ausdruck (`print.jpg`/Drucker).
- Bezeichner, Kommentare und UI-Texte auf Deutsch, im Stil des bestehenden Codes (`_feld`, deutsche Methodennamen).
- UI-Texte wörtlich: „Seite wird gedruckt …“, „{n} Bilder werden aufbereitet“, „Doppelklick: Ausschnitt bearbeiten“, „Doppelklick auf ein Bild zum Bearbeiten“, „Bildausschnitt bearbeiten – {Dateiname}“, „Ziehen = verschieben · Mausrad = zoomen · {n} Gesichter erkannt“, „Automatisch“, „Abbrechen“, „OK“, „Druckrand {d} mm“, „sichtbar {d − 8} mm“.

## Review Focus

1. Foto wird noch von EOS Utility geschrieben, wenn die Erkennung läuft → keine Begleitdatei schreiben, im nächsten Durchlauf erneut versuchen (Test in Task 5).
2. Begleitdatei leer oder beschädigt (Absturz, halbe Datei) → wie „nicht vorhanden“ behandeln, Foto neu erkennen, kein Absturz (Test in Task 3).
3. Foto verschwindet, während das Bearbeiten-Fenster offen ist (gelöscht/verschoben) → Speichern wird ignoriert, keine verwaiste Begleitdatei (Test in Task 3).
4. Durchmesser oder Prozent wird nach einer Handkorrektur geändert → Handkorrektur bleibt unverändert, automatische Ausschnitte folgen der Einstellung (Test in Task 3).
5. Archivieren schlägt mittendrin fehl (Datei gesperrt) → Fotos **und** Begleitdateien zurück in den überwachten Ordner, leerer Zielordner gelöscht (Test in Task 5).

---

### Task 1: Testprojekt, Ziel-Framework und Ausschnitt-Rechnung

**Files:**
- Modify: `PrintImageForButtonMachine/PrintImageForButtonMachine.csproj` (TargetFramework, `InternalsVisibleTo`)
- Create: `PrintImageForButtonMachine/Ausschnitt.cs`
- Create: `PrintImageForButtonMachine.Tests/PrintImageForButtonMachine.Tests.csproj`
- Create: `PrintImageForButtonMachine.Tests/AusschnittRechnerTests.cs`
- Modify: `PrintImageForButtonMachine.sln` (Testprojekt hinzufügen)

**Interfaces:**
- Produces (Namespace `PrintImageForButtonMachine`, alle `internal`):
  - `record Gesicht(double X, double Y, double Breite, double Hoehe)` – relativ zu Bildbreite/-höhe (0..1)
  - `record Ausschnitt(double MitteX, double MitteY, double Seite)` – Mitte relativ zu Breite/Höhe, `Seite` relativ zur kürzeren Kante
  - `static class AusschnittRechner`:
    - `const double RandCm = 0.4`
    - `Ausschnitt Automatisch(int breite, int hoehe, IReadOnlyList<Gesicht> gesichter, double faktor, double durchmesserCm)` (`faktor` 0..1)
    - `Ausschnitt Begrenze(Ausschnitt a, int breite, int hoehe)`
    - `Ausschnitt Zoome(Ausschnitt a, double faktor, double punktX, double punktY)` – `Seite *= faktor`, relativer Bildpunkt bleibt an seiner Bildschirmposition; **ohne** Begrenzung
    - `RectangleF InPixel(Ausschnitt a, int breite, int hoehe)` – Quell-Rechteck in Bildpixeln

- [ ] **Step 1: Ziel-Framework umstellen und Testprojekt anlegen**

Hauptprojekt: `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>` und
`<ItemGroup><InternalsVisibleTo Include="PrintImageForButtonMachine.Tests" /></ItemGroup>`.
Testprojekt: xUnit (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`), gleiches Ziel-Framework, `<UseWindowsForms>true</UseWindowsForms>`, `ProjectReference` auf das Hauptprojekt. Mit `dotnet sln add` in die Solution aufnehmen.

Run: `dotnet build`
Expected: Build erfolgreich, 0 Fehler.

- [ ] **Step 2: Failing tests schreiben** (`AusschnittRechnerTests`, Toleranz 1e-4, Bild 3000 × 2000, `faktor` 0.8, Durchmesser 5.2 cm)

```csharp
[Fact] void OhneGesichter_Mittig()                 // Automatisch(3000,2000,[],0.8,5.2) == (0.5, 0.5, 0.8)
[Fact] void OhneGesichter_Hochformat_Mittig()      // Automatisch(2000,3000,[],0.8,5.2) == (0.5, 0.5, 0.8)
[Fact] void EinGesicht_MitteWandert_GroesseBleibt()
    // Gesicht(0.30, 0.45, 0.10, 0.15) → (0.35, 0.50625, 0.8)
[Fact] void EinGesicht_AmOberenRand_WirdBegrenzt()
    // Gesicht(0.45, 0.30, 0.10, 0.15) → (0.5, 0.4, 0.8)
[Fact] void ZweiGesichter_ZoomtHeraus()
    // Gesichter(0.25,0.45,0.10,0.15) und (0.65,0.45,0.10,0.15) → (0.5, 0.50625, 0.97890) (Toleranz 1e-3)
[Fact] void DreiGesichter_HoechstensKuerzereKante()
    // Gesichter x = 0.20 / 0.45 / 0.70, je y 0.45, 0.10 × 0.15 → (0.5, 0.5, 1.0)
[Fact] void KleinesGesicht_ZoomtNieHinein()
    // Gesicht(0.49, 0.45, 0.02, 0.03) → Seite == 0.8
[Fact] void Begrenze_SchiebtInsBild()              // Begrenze((0.95, 0.5, 0.8), 3000, 2000) == (0.73333, 0.5, 0.8)
[Fact] void Begrenze_SeiteZwischen10Und100Prozent() // Seite 1.5 → 1.0, Seite 0.01 → 0.1
[Fact] void Zoome_UmMitte()                         // Zoome((0.5,0.5,0.8), 0.5, 0.5, 0.5) == (0.5, 0.5, 0.4)
[Fact] void Zoome_UmPunkt_PunktBleibtStehen()        // Zoome((0.5,0.5,0.8), 0.5, 0.7, 0.5) == (0.6, 0.5, 0.4)
[Fact] void InPixel_Rechteck()                       // InPixel((0.5,0.5,0.8), 3000, 2000) == RectangleF(700, 200, 1600, 1600)
```

- [ ] **Step 3: Tests laufen lassen**

Run: `dotnet test`
Expected: FAIL (Typen existieren nicht / Kompilierfehler).

- [ ] **Step 4: `Ausschnitt.cs` implementieren**

`Automatisch` – Rechnung in Pixeln (`kurz = min(breite, hoehe)`, `innenAnteil = (d − 2·RandCm) / d`):

```
seite = kurz * faktor; cx = breite/2; cy = hoehe/2
wenn Gesichter vorhanden:
    l = min(x − 0.15·w), r = max(x + w + 0.15·w), o = min(y − 0.40·h), u = max(y + h + 0.15·h)   (alles in Pixeln)
    cx = (l+r)/2; cy = (o+u)/2
    benoetigt = 2 · sqrt(((r−l)/2)² + ((u−o)/2)²) / innenAnteil
    wenn benoetigt > seite: seite = min(benoetigt, kurz)
Ergebnis relativ, danach Begrenze
```

`Begrenze`: `Seite` auf [0.1, 1] klemmen, dann Mitte so, dass das Quadrat im Bild liegt.
`Zoome`: `neu = punkt + (mitte − punkt) · faktor` je Achse, `Seite · faktor`.

- [ ] **Step 5: Tests laufen lassen**

Run: `dotnet test`
Expected: alle Tests PASS.

- [ ] **Step 6: Commit**

```bash
git add PrintImageForButtonMachine.sln PrintImageForButtonMachine/PrintImageForButtonMachine.csproj PrintImageForButtonMachine/Ausschnitt.cs PrintImageForButtonMachine.Tests
git commit -m "Ausschnitt-Rechnung mit Gesichtern und Testprojekt"
```

---

### Task 2: Seitenlayout mit Trefferprüfung

**Files:**
- Create: `PrintImageForButtonMachine/SeitenLayout.cs`
- Modify: `PrintImageForButtonMachine/ImageProcessor.cs` (Positionsrechnung in `GeneriereBild`, `ErmittleAnzahlBilder`, `ErmittleAnzahlBilderProSeite` durch `SeitenLayout` ersetzen; Konstanten `SeitenBreiteCm`, `SeitenHoeheCm`, `AbstandCm` wandern dorthin)
- Test: `PrintImageForButtonMachine.Tests/SeitenLayoutTests.cs`

**Interfaces:**
- Produces: `internal class SeitenLayout(double groesseCm)` mit
  - `int Spalten`, `int Zeilen`, `int BilderProSeite`
  - `RectangleF KreisRechteckCm(int index)` – `x = 0.5 + (index % Spalten)·(g + 0.5)`, `y = 0.5 + (index / Spalten)·(g + 0.5)`, Breite/Höhe `g`
  - `int? IndexAn(double xCm, double yCm, int anzahlBilder)` – Index des Kreises, in dessen **Kreisfläche** der Punkt liegt, sonst `null`

- [ ] **Step 1: Failing tests** (Durchmesser 5.2 cm)

```csharp
[Fact] void Raster_52mm()              // Spalten 3, Zeilen 5, BilderProSeite 15
[Fact] void Treffer_Kreismitte0()      // IndexAn(3.1, 3.1, 5) == 0
[Fact] void Treffer_Kreismitte4()      // IndexAn(8.8, 8.8, 5) == 4
[Fact] void KeinTreffer_Zwischenraum() // IndexAn(5.95, 3.1, 5) == null
[Fact] void KeinTreffer_EckeAusserhalbKreis() // IndexAn(0.6, 0.6, 5) == null
[Fact] void KeinTreffer_OhneBild()     // IndexAn(8.8, 8.8, 4) == null
[Fact] void KreisRechteck_Index4()     // KreisRechteckCm(4) == (6.2, 6.2, 5.2, 5.2)
```

- [ ] **Step 2: Run `dotnet test`** – Expected: FAIL (Kompilierfehler).
- [ ] **Step 3: `SeitenLayout` implementieren und `ImageProcessor` darauf umstellen** (Positionen weiter erst in cm rechnen, dann mit `InPixel` umrechnen).
- [ ] **Step 4: Run `dotnet test`** – Expected: PASS. Zusätzlich `dotnet run --project PrintImageForButtonMachine`, Vorschau mit `test-bilder` sieht unverändert aus.
- [ ] **Step 5: Commit** – `git commit -m "Seitenlayout mit Trefferprüfung"`

---

### Task 3: Begleitdatei und Ausschnitt-Wahl

**Files:**
- Create: `PrintImageForButtonMachine/BildInfo.cs`
- Test: `PrintImageForButtonMachine.Tests/BildInfoTests.cs`

**Interfaces:**
- Consumes: `Gesicht`, `Ausschnitt`, `AusschnittRechner` (Task 1)
- Produces:
  - `internal record BildInfo(List<Gesicht> Gesichter, Ausschnitt? Manuell)`
  - `internal static class BildInfoSpeicher`:
    - `string PfadFuer(string fotoPfad)` → `Path.ChangeExtension(fotoPfad, ".button.json")`
    - `BildInfo? Lade(string fotoPfad)` – `null`, wenn Datei fehlt, leer oder kein gültiges JSON
    - `bool Speichere(string fotoPfad, BildInfo info)` – `false` und nichts schreiben, wenn das Foto nicht (mehr) existiert; schreibt über temporäre Datei + `File.Move(…, overwrite: true)`
  - in `AusschnittRechner` ergänzen: `Ausschnitt Waehle(BildInfo? info, int breite, int hoehe, double faktor, double durchmesserCm)` → `Begrenze(info.Manuell)` wenn vorhanden, sonst `Automatisch(…, info?.Gesichter ?? [], …)`

- [ ] **Step 1: Failing tests** (temporäres Verzeichnis je Test)

```csharp
[Fact] void PfadFuer_EndetNichtAufJpg()     // PfadFuer(@"C:\a\IMG_1.JPG") == @"C:\a\IMG_1.button.json"
[Fact] void SpeichernUndLaden()             // Gesichter + Manuell round-trip, Werte gleich
[Fact] void Lade_OhneDatei_Null()
[Fact] void Lade_LeereDatei_Null()          // Review Focus 2
[Fact] void Lade_KaputtesJson_Null()        // Inhalt "{\"Gesichter\": [" → null
[Fact] void Speichere_FotoFehlt_FalseUndKeineDatei()  // Review Focus 3
[Fact] void Waehle_Manuell_IgnoriertEinstellungen()   // Review Focus 4: gleiches Ergebnis für (0.8, 5.2) und (0.5, 3.8)
[Fact] void Waehle_Automatisch_FolgtFaktor()          // ohne Manuell: Seite 0.8 bei faktor 0.8, 0.5 bei faktor 0.5
```

- [ ] **Step 2: Run `dotnet test`** – Expected: FAIL.
- [ ] **Step 3: Implementieren** (System.Text.Json, eingerückt; `JsonException`/`IOException` beim Laden → `null`).
- [ ] **Step 4: Run `dotnet test`** – Expected: PASS.
- [ ] **Step 5: Commit** – `git commit -m "Begleitdatei für Gesichter und Handkorrektur"`

---

### Task 4: Bilder richtig ausgerichtet laden (EXIF)

**Files:**
- Create: `PrintImageForButtonMachine/BildLader.cs`
- Modify: `PrintImageForButtonMachine/ImageProcessor.cs` (privates `LadeBild` entfernen, Aufrufe auf `BildLader.LadeBild`)
- Test: `PrintImageForButtonMachine.Tests/BildLaderTests.cs`

**Interfaces:**
- Produces: `internal static class BildLader`
  - `Image? LadeBild(string pfad)` – bisheriges Verhalten (komplett in den Speicher, `null` bei `IOException`/`UnauthorizedAccessException`/`ArgumentException`/`OutOfMemoryException`) **plus** EXIF-Tag `0x0112` auswerten, `RotateFlip` anwenden und den Tag entfernen
  - `RotateFlipType DrehungFuerExif(int orientierung)`

- [ ] **Step 1: Failing tests**

```csharp
[Theory]
[InlineData(1, RotateFlipType.RotateNoneFlipNone)] [InlineData(2, RotateFlipType.RotateNoneFlipX)]
[InlineData(3, RotateFlipType.Rotate180FlipNone)]  [InlineData(4, RotateFlipType.Rotate180FlipX)]
[InlineData(5, RotateFlipType.Rotate90FlipX)]      [InlineData(6, RotateFlipType.Rotate90FlipNone)]
[InlineData(7, RotateFlipType.Rotate270FlipX)]     [InlineData(8, RotateFlipType.Rotate270FlipNone)]
[InlineData(0, RotateFlipType.RotateNoneFlipNone)] [InlineData(99, RotateFlipType.RotateNoneFlipNone)]
void DrehungFuerExif(int o, RotateFlipType erwartet)

[Fact] void LadeBild_AbgeschnitteneDatei_Null()   // erste 100 Bytes eines gültigen JPEG → null
[Fact] void LadeBild_OhneExif_Unveraendert()       // 30×20-Bitmap als JPEG gespeichert → 30×20
```

- [ ] **Step 2: Run `dotnet test`** – Expected: FAIL.
- [ ] **Step 3: Implementieren.**
- [ ] **Step 4: Run `dotnet test`** – Expected: PASS.
- [ ] **Step 5: Commit** – `git commit -m "EXIF-Ausrichtung beim Laden auswerten"`

---

### Task 5: Gesichtserkennung und Ausschnitt in Vorschau und Druck

**Files:**
- Create: `PrintImageForButtonMachine/Gesichtserkennung.cs`
- Modify: `PrintImageForButtonMachine/ImageProcessor.cs`
- Test: `PrintImageForButtonMachine.Tests/GesichtserkennungTests.cs`, `PrintImageForButtonMachine.Tests/ArchivTests.cs`

**Interfaces:**
- Consumes: `BildLader.LadeBild`, `BildInfoSpeicher`, `AusschnittRechner.Waehle/InPixel`, `SeitenLayout`
- Produces:
  - `internal static class Gesichtserkennung`
    - `Task<List<Gesicht>> ErkenneAsync(Image bild)` – leere Liste, wenn `FaceDetector.IsSupported` false ist oder die Erkennung eine Ausnahme wirft
    - `Task ErgaenzeFehlendeAsync(IEnumerable<FileInfo> fotos)` – für jedes Foto ohne gültige Begleitdatei: laden; wenn `null` → überspringen (nächster Durchlauf); sonst erkennen und `BildInfo(gesichter, null)` speichern
  - `ImageProcessor`:
    - `internal static void Archiviere(FileInfo[] fotos, string ordnerPfad)` – verschiebt Fotos samt vorhandenen Begleitdateien; bei `IOException`/`UnauthorizedAccessException` alles zurück, leeren Zielordner löschen, Ausnahme weiterwerfen
    - `public FileInfo[] VorschauDateien { get; private set; }` – Dateien in der Reihenfolge der zuletzt erzeugten Vorschau
    - `public bool BearbeitungOffen { get; set; }` – solange `true`, druckt `PruefeOrdnerAsync` nicht automatisch (aktualisiert nur die Vorschau)
    - `public double GroesseCm => _groesse`, `public double FaktorAnteil => _faktor` (für Task 7/8)

Gesichtserkennung – Umwandlung nach Gray8 (nicht offensichtlich, deshalb vorgegeben):

```csharp
// verkleinerte 32bpp-Kopie mit langer Kante 1500 px → Grauwerte → SoftwareBitmap
var grau = new byte[kw * kh];   // aus LockBits/Marshal.Copy: (R*77 + G*150 + B*29) >> 8
var sb = new SoftwareBitmap(BitmapPixelFormat.Gray8, kw, kh);
sb.CopyFromBuffer(grau.AsBuffer());   // using System.Runtime.InteropServices.WindowsRuntime;
var faces = await (await FaceDetector.CreateAsync()).DetectFacesAsync(sb);
// FaceBox (BitmapBounds, Pixel der Kopie) → Gesicht relativ: X / kw, Y / kh, Width / kw, Height / kh
```

Den `FaceDetector` einmal erzeugen und wiederverwenden (statisches `Lazy<Task<FaceDetector>>`). Verkleinern und Graustufen in `Task.Run`.

- [ ] **Step 1: Failing tests**

```csharp
// GesichtserkennungTests
[Fact] async Task ErgaenzeFehlende_UnvollstaendigesFoto_KeineBegleitdatei()  // Review Focus 1: abgeschnittenes JPEG → keine .button.json
[Fact] async Task ErgaenzeFehlende_FotoOhneGesicht_BegleitdateiMitLeererListe() // einfarbige 400×300-JPEG → Gesichter.Count == 0
[Fact] async Task ErgaenzeFehlende_VorhandeneBegleitdatei_BleibtUnveraendert()   // Manuell bleibt erhalten

// ArchivTests
[Fact] void Archiviere_VerschiebtBegleitdateienMit()
[Fact] void Archiviere_GesperrteDatei_AllesZurueck()  // Review Focus 5: zweites Foto per FileShare.None geöffnet
    // → IOException; beide Fotos + Begleitdateien wieder im Quellordner; Zielordner existiert nicht
```

- [ ] **Step 2: Run `dotnet test`** – Expected: FAIL.
- [ ] **Step 3: `Gesichtserkennung` implementieren.**
- [ ] **Step 4: `ImageProcessor` umbauen**
  - `GeneriereVorschauAsync` und `BearbeiteOrdnerAsync` rufen innerhalb ihres `_beschaeftigt`-Abschnitts zuerst `Gesichtserkennung.ErgaenzeFehlendeAsync` auf (vor dem Rendern bzw. vor dem Archivieren).
  - `ErzeugeSignatur` nimmt je Foto auch `LastWriteTimeUtc.Ticks` der Begleitdatei auf (fehlend = 0).
  - `GeneriereBild`: je Foto `BildInfoSpeicher.Lade` → `AusschnittRechner.Waehle(info, bild.Width, bild.Height, _faktor, _groesse)` → `ZeichneKreisausschnitt(g, bild, ausschnitt, zielRechteck)` mit `InPixel` als Quellrechteck. Nur bei `dpi < DruckDpi` und `info?.Manuell != null`: blauer Kreis (Ø 0,45 cm, `Color.FromArgb(0, 120, 212)`) mit weißem „✎“ unten rechts am Kreis.
  - Verschiebeschleife in `BearbeiteOrdnerAsync` durch `Archiviere` ersetzen (Meldungstext bei Fehler bleibt).
  - `PruefeOrdnerAsync`: automatischer Druck nur, wenn `!BearbeitungOffen`.
- [ ] **Step 5: Run `dotnet test`** – Expected: PASS.
- [ ] **Step 6: Manuell prüfen** – `test-bilder` in einen leeren Ordner kopieren, als überwachten Ordner wählen, Überwachung starten: Nach wenigen Sekunden liegen 10 `.button.json` daneben, die Vorschau zeigt die Ausschnitte wie in `test-bilder/_facespike/_uebersicht.jpg`. Prozent auf 60 ändern → Vorschau ändert sich.
- [ ] **Step 7: Commit** – `git commit -m "Gesichtserkennung und Ausschnitt in Vorschau und Druck"`

---

### Task 6: Rückmeldung beim Drucken

**Files:**
- Modify: `PrintImageForButtonMachine/ImageProcessor.cs`, `PrintImageForButtonMachine/Form1.cs`, `PrintImageForButtonMachine/Form1.Designer.cs`

**Interfaces:**
- Produces in `ImageProcessor`:
  - `public event EventHandler<int>? DruckGestartet` (Argument: Anzahl Fotos) – ausgelöst in `BearbeiteOrdnerAsync`, sobald feststeht, dass gedruckt wird (Fotos vorhanden), noch vor der Erkennung
  - `public event EventHandler? DruckBeendet` – im `finally`, nur wenn `DruckGestartet` ausgelöst wurde

- [ ] **Step 1: `printDocument.Print()` in `DruckeBild` per `Task.Run` ausführen** (`DruckeBild` wird `async Task`), Fehlermeldungen danach im UI-Thread. Grund: Der Druck-Spooler blockiert sonst die Oberfläche, und die Überlagerung würde nicht gezeichnet.
- [ ] **Step 2: Events auslösen** wie im Interfaces-Block.
- [ ] **Step 3: Überlagerung in `Form1`**
  - Bei `DruckGestartet`: `btn_startStop`, `btn_print`, `button1` gesperrt; `btn_print.Text = "Seite wird gedruckt …"`; `UseWaitCursor = true`; Feld `_druckAnzahl = n`; eine `ProgressBar` (`Style = Marquee`, Breite 180, als Kind von `pictureBox1`, mittig unter dem Text) sichtbar; `pictureBox1.Invalidate()`.
  - `pictureBox1.Paint`: wenn Druck läuft, Fläche mit `Color.FromArgb(210, Color.White)` füllen, mittig fett „Seite wird gedruckt …“ und darunter „{n} Bilder werden aufbereitet“.
  - Bei `DruckBeendet`: alles zurück (`btn_print.Text = "Jetzt drucken"`).
- [ ] **Step 4: Manuell prüfen** – Drucker „Microsoft Print to PDF“, 10 Testfotos, „Jetzt drucken“: Überlagerung erscheint sofort, Balken läuft, Knöpfe gesperrt, Wartecursor; danach Normalzustand. Dasselbe beim automatischen Druck (Seite voll machen).
- [ ] **Step 5: Run `dotnet test`** – Expected: PASS (keine Regression).
- [ ] **Step 6: Commit** – `git commit -m "Sichtbare Rückmeldung während des Druckens"`

---

### Task 7: Bearbeiten-Fenster

**Files:**
- Create: `PrintImageForButtonMachine/AusschnittForm.cs` (nur Code, ohne Designer-Datei)

**Interfaces:**
- Consumes: `Ausschnitt`, `AusschnittRechner.Begrenze/Zoome/InPixel`
- Produces: `internal class AusschnittForm : Form`
  - `AusschnittForm(Image bild, string dateiname, Ausschnitt start, Ausschnitt automatisch, double durchmesserCm, int anzahlGesichter)`
  - `Ausschnitt? Ergebnis { get; }` – nach `DialogResult.OK`: `null`, wenn der aktuelle Ausschnitt dem automatischen entspricht (nach „Automatisch“ nicht mehr bewegt), sonst der Ausschnitt

Festlegungen:
- Titel „Bildausschnitt bearbeiten – {dateiname}“, Startgröße ca. 680 × 560, größenveränderbar, `StartPosition = CenterParent`.
- Zeichenfläche: doppelt gepuffertes Panel, Hintergrund `#2B2B2B`. Druckkreis-Durchmesser auf dem Bildschirm = 80 % der kürzeren Seite der Fläche, mittig, fest.
- Für die Anzeige eine Arbeitskopie des Fotos mit max. 2.500 px langer Kante (Tempo auf dem alten Notebook); `Ausschnitt` ist relativ, daher unabhängig davon.
- Abdunklung außerhalb des Druckkreises `Color.FromArgb(150, 0, 0, 0)`; Druckkreis weiß 2 px; sichtbarer Kreis gelb (`#FFD400`) gestrichelt 2 px mit Durchmesser · (d − 0,8)/d; Beschriftungen „Druckrand {d} mm“ und „sichtbar {d − 8} mm“ (mm gerundet).
- Ziehen mit linker Maustaste verschiebt; Mausrad: `Zoome(a, Math.Pow(1.1, -delta / 120.0), punktUnterMaus…)`; nach jeder Änderung `Begrenze`.
- Unten: Hinweis „Ziehen = verschieben · Mausrad = zoomen · {n} Gesichter erkannt“, Knöpfe „Automatisch“, „Abbrechen“ (`CancelButton`), „OK“ (`AcceptButton`).

- [ ] **Step 1: Implementieren.**
- [ ] **Step 2: Manuell prüfen** (vorübergehend über Task 8 oder einen Debug-Aufruf): Ziehen/Zoomen flüssig; Herauszoomen stoppt, bevor Weiß sichtbar wird; „Automatisch“ springt zurück; Enter = OK, Esc = Abbrechen.
- [ ] **Step 3: Run `dotnet test`** – Expected: PASS.
- [ ] **Step 4: Commit** – `git commit -m "Bearbeiten-Fenster für den Bildausschnitt"`

---

### Task 8: Vorschau – Hover, Doppelklick, Texte

**Files:**
- Modify: `PrintImageForButtonMachine/Form1.cs`, `PrintImageForButtonMachine/Form1.Designer.cs`, `PrintImageForButtonMachine/Form1.resx`

**Interfaces:**
- Consumes: `SeitenLayout`, `ImageProcessor.VorschauDateien/BearbeitungOffen/GroesseCm/FaktorAnteil/GeneriereVorschauAsync`, `BildLader`, `BildInfoSpeicher`, `AusschnittRechner.Automatisch/Waehle`, `AusschnittForm`

- [ ] **Step 1: Umrechnung Vorschaupunkt → cm** in `Form1`: `int? KreisIndexAn(Point p)` – Bild wird mit `SizeMode.Zoom` angezeigt: `skala = min(pbB/bildB, pbH/bildH)`, `versatz = ((pbB − bildB·skala)/2, (pbH − bildH·skala)/2)`, `cm = (p − versatz) / skala / bild.HorizontalResolution · 2.54`; dann `new SeitenLayout(processor.GroesseCm).IndexAn(…, processor.VorschauDateien.Length)`. Umkehrung für den Hover-Rahmen entsprechend.
- [ ] **Step 2: Hover** – `MouseMove`: Index merken, `Cursor = Cursors.Hand`, `ToolTip` „Doppelklick: Ausschnitt bearbeiten“, `Invalidate`; `Paint` zeichnet einen 2-px-Rahmen `#0078D4` 2 px außerhalb des Kreises. `MouseLeave` setzt zurück.
- [ ] **Step 3: Doppelklick** – Foto über `BildLader.LadeBild` laden (bei `null` nichts tun), `BildInfo` laden, `automatisch = AusschnittRechner.Automatisch(…)`, `start = Waehle(…)`; `processor.BearbeitungOffen = true`; `AusschnittForm.ShowDialog(this)`; im `finally` `BearbeitungOffen = false`. Bei OK `BildInfoSpeicher.Speichere(foto, info with { Manuell = form.Ergebnis })` (fehlende Info → `new BildInfo([], form.Ergebnis)`), dann `await processor.GeneriereVorschauAsync(true)`.
- [ ] **Step 4: Texte** – `label_Uhrzeit` zeigt „Zuletzt aktualisiert: HH:mm:ss · Doppelklick auf ein Bild zum Bearbeiten“ (Text in `ImageProcessor.GeneriereVorschauAsync`); Einleitungstext in `Form1.resx` um die automatische Ausrichtung und den Doppelklick ergänzen, z. B. „Neue Fotos werden automatisch auf die Gesichter ausgerichtet. Mit einem Doppelklick auf ein Bild in der Vorschau lässt sich der Ausschnitt anpassen.“
- [ ] **Step 5: Manuell prüfen** – mit `test-bilder`: Hover-Rahmen und Tooltip über Kreisen, nicht im Zwischenraum; Doppelklick öffnet das richtige Foto; nach OK erscheint der neue Ausschnitt mit Stift-Symbol; nach „Automatisch“ + OK verschwindet das Stift-Symbol; bei offener Bearbeitung und voller Seite wird nicht automatisch gedruckt; `print.jpg` enthält weder Stift noch Rahmen.
- [ ] **Step 6: Run `dotnet test`** – Expected: PASS.
- [ ] **Step 7: Commit** – `git commit -m "Vorschau: Doppelklick zum Bearbeiten"`

---

### Task 9: README, Version, Auslieferung prüfen

**Files:**
- Modify: `README.md`, `PrintImageForButtonMachine/PrintImageForButtonMachine.csproj` (`<Version>1.2.0</Version>`)

- [ ] **Step 1: README** – Funktionen um automatische Ausrichtung, Bearbeiten per Doppelklick (Hilfskreis 4 mm), Begleitdateien `.button.json` (wandern beim Archivieren mit) und Druckanzeige ergänzen; Einstellung „Größe des Bildausschnitts (%)“ neu beschreiben (Größe des Ausschnitts, Mitte folgt den Gesichtern); Voraussetzung „Windows 10 (Version 2004) oder 11“.
- [ ] **Step 2: Publish prüfen**

Run: `dotnet publish PrintImageForButtonMachine -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
Expected: Erfolg; die exe startet auf dem Entwicklungs-PC und erkennt Gesichter. Größe der exe mit Version 1.1.0 vergleichen und im Abschlussbericht nennen.
- [ ] **Step 3: Commit** – `git commit -m "Version 1.2.0: automatische Ausrichtung und Bearbeiten-Fenster"`
