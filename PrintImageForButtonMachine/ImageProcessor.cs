using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintImageForButtonMachine
{
    internal class ImageProcessor
    {
        private const double cmProInch = 2.54;
        private const int VorschauDpi = 96; // genügt für die Bildschirmvorschau
        private const int DruckDpi = 300; // mehr bringt auch bei 600-dpi-Laserdruckern keinen sichtbaren Gewinn
        private TextBox _ueberwachungPfad;
        private TextBox _ausgabePfad;
        private double _groesse; // cm
        private PictureBox _pictureBox;
        private Label _labelAktualisiert;
        private readonly ComboBox _comboBox;
        private string? _vorschauSignatur; // Stand der zuletzt erzeugten Vorschau
        private bool _beschaeftigt; // verhindert parallele Verarbeitung (nur im UI-Thread verwendet)

        public string UeberwachungPfad { get => _ueberwachungPfad.Text; set => _ueberwachungPfad.Text = value; }
        public string AusgabePfad { get => _ausgabePfad.Text; set => _ausgabePfad.Text = value; }
        public double Groesse { get => _groesse; set => _groesse = value; }
        public double GroesseCm => _groesse;

        /// <summary>
        /// Fotos in der Reihenfolge der zuletzt erzeugten Vorschau.
        /// </summary>
        public FileInfo[] VorschauDateien { get; private set; } = [];

        /// <summary>
        /// Solange ein Foto bearbeitet wird, druckt die Überwachung nicht automatisch.
        /// </summary>
        public bool BearbeitungOffen { get; set; }

        /// <summary>
        /// Eine Seite wird aufbereitet und gedruckt; Argument ist die Anzahl der Fotos.
        /// </summary>
        public event EventHandler<int>? DruckGestartet;
        public event EventHandler? DruckBeendet;

        public ImageProcessor(TextBox p_UeberwachungPfad, TextBox p_ausgabePfad, double p_Groesse, PictureBox p_PictureBox, Label labelAktualisiert, ComboBox comboBox)
        {
            _ueberwachungPfad = p_UeberwachungPfad;
            _ausgabePfad = p_ausgabePfad;
            _groesse = p_Groesse;
            _pictureBox = p_PictureBox;
            _labelAktualisiert = labelAktualisiert;
            _comboBox = comboBox;
        }

        /// <summary>
        /// Druckt, sobald genügend Bilder für eine Seite vorhanden sind, ansonsten wird die Vorschau aktualisiert.
        /// </summary>
        public async Task PruefeOrdnerAsync()
        {
            FileInfo[] dateien = LeseBilder(UeberwachungPfad);
            if (!BearbeitungOffen && dateien.Length > 0 && dateien.Length >= ErmittleAnzahlBilderProSeite())
            {
                await BearbeiteOrdnerAsync();
            }
            else
            {
                await GeneriereVorschauAsync(false);
            }
        }

        public async Task BearbeiteOrdnerAsync()
        {
            FileInfo[] vorab = ErsteSeite(LeseBilder(UeberwachungPfad), _groesse);
            if (vorab.Length == 0) return;
            DruckGestartet?.Invoke(this, vorab.Length);
            try
            {
                // Läuft gerade eine Vorschau oder ein anderer Druck, wird gewartet, statt den Auftrag zu verschlucken
                while (_beschaeftigt) await Task.Delay(100);
                await DruckeSeiteAsync();
            }
            finally
            {
                DruckBeendet?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task DruckeSeiteAsync()
        {
            _beschaeftigt = true;
            try
            {
                // Nur eine Seite; weitere Fotos bleiben für die nächste Seite im Ordner
                FileInfo[] files = ErsteSeite(LeseBilder(UeberwachungPfad), _groesse);
                if (files.Length == 0) return;
                await Gesichtserkennung.ErgaenzeFehlendeAsync(files);

                // verschiebe Dateien in eigenen Ordner
                string ordnerName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string ordnerPfad = Path.Combine(AusgabePfad, ordnerName);
                try
                {
                    Archiviere(files, ordnerPfad);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show("Eine Datei im überwachten Ordner konnte nicht kopiert werden, da sie von einem anderen Programm blockiert wird. Möglicherweise hat das Aufnahmeprogramm noch eine Vorschau geöffnet. ", "Bearbeitung nicht möglich", MessageBoxButtons.OK);
                    return;
                }

                // Bild im Hintergrund erzeugen und speichern, damit die Oberfläche nicht blockiert
                using Bitmap neuesBild = await Task.Run(() =>
                {
                    Bitmap bild = GeneriereBild(LeseBilder(ordnerPfad), DruckDpi, out _);
                    string printPath = Path.Combine(ordnerPfad, "print.jpg");
                    if (File.Exists(printPath))
                    {
                        File.Delete(printPath);
                    }
                    bild.Save(printPath, ImageFormat.Jpeg);
                    return bild;
                });
                await DruckeBildAsync(neuesBild);
                _vorschauSignatur = null;
            }
            finally
            {
                _beschaeftigt = false;
            }
        }

        /// <summary>
        /// Verschiebt die Fotos samt Begleitdateien in den Zielordner. Schlägt das fehl, wird alles zurückverschoben
        /// und die Ausnahme weitergegeben.
        /// </summary>
        internal static void Archiviere(FileInfo[] fotos, string ordnerPfad)
        {
            var verschobeneDateien = new List<(string Quelle, string Ziel)>();
            try
            {
                Directory.CreateDirectory(ordnerPfad);
                foreach (FileInfo foto in fotos)
                {
                    string begleitdatei = BildInfoSpeicher.PfadFuer(foto.FullName);
                    string quelle = foto.FullName;
                    string ziel = Path.Combine(ordnerPfad, foto.Name);
                    File.Move(quelle, ziel);
                    verschobeneDateien.Add((quelle, ziel));
                    if (File.Exists(begleitdatei))
                    {
                        string begleitZiel = Path.Combine(ordnerPfad, Path.GetFileName(begleitdatei));
                        File.Move(begleitdatei, begleitZiel);
                        verschobeneDateien.Add((begleitdatei, begleitZiel));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Verschiebe bereits verschobene Dateien zurück
                foreach (var (quelle, ziel) in verschobeneDateien)
                {
                    File.Move(ziel, quelle);
                }
                if (Directory.Exists(ordnerPfad) && !Directory.EnumerateFileSystemEntries(ordnerPfad).Any())
                {
                    Directory.Delete(ordnerPfad);
                }
                throw;
            }
        }

        /// <param name="erzwingen">Vorschau auch dann neu erzeugen, wenn sich nichts geändert hat.</param>
        public async Task GeneriereVorschauAsync(bool erzwingen)
        {
            if (_beschaeftigt) return;
            _beschaeftigt = true;
            try
            {
                FileInfo[] dateien = ErsteSeite(LeseBilder(UeberwachungPfad), _groesse);
                await Gesichtserkennung.ErgaenzeFehlendeAsync(dateien);
                string signatur = ErzeugeSignatur(dateien);
                if (erzwingen || signatur != _vorschauSignatur)
                {
                    bool vollstaendig = false;
                    Bitmap vorschaubild = await Task.Run(() => GeneriereBild(dateien, VorschauDpi, out vollstaendig));
                    Image? altesBild = _pictureBox.Image;
                    _pictureBox.Image = vorschaubild;
                    altesBild?.Dispose();
                    VorschauDateien = dateien;
                    // Nicht ladbare Bilder (z. B. noch im Schreibvorgang) beim nächsten Durchlauf erneut versuchen
                    _vorschauSignatur = vollstaendig ? signatur : null;
                }
                _labelAktualisiert.Text = $"Zuletzt aktualisiert: {DateTime.Now.ToString("HH:mm:ss")}\nDoppelklick auf ein Bild zum Bearbeiten";
            }
            finally
            {
                _beschaeftigt = false;
            }
        }

        /// <summary>
        /// Die Fotos, die auf eine Seite passen, in der Reihenfolge von Vorschau und Druck.
        /// </summary>
        internal static FileInfo[] ErsteSeite(FileInfo[] dateien, double groesseCm)
        {
            return dateien.Take(new SeitenLayout(groesseCm).BilderProSeite).ToArray();
        }

        public int ErmittleAnzahlBilderProSeite()
        {
            return new SeitenLayout(_groesse).BilderProSeite;
        }

        private static FileInfo[] LeseBilder(string ordnerPfad)
        {
            return new DirectoryInfo(ordnerPfad).GetFiles("*.jpg");
        }

        private string ErzeugeSignatur(FileInfo[] dateien)
        {
            var signatur = new StringBuilder();
            signatur.Append(_groesse);
            foreach (FileInfo datei in dateien)
            {
                signatur.Append('|').Append(datei.Name).Append(':').Append(datei.Length).Append(':').Append(datei.LastWriteTimeUtc.Ticks);
                // Neue Erkennung oder Handkorrektur ändert die Begleitdatei und damit die Vorschau
                var begleitdatei = new FileInfo(BildInfoSpeicher.PfadFuer(datei.FullName));
                signatur.Append(':').Append(begleitdatei.Exists ? begleitdatei.LastWriteTimeUtc.Ticks : 0);
            }
            return signatur.ToString();
        }

        /// <param name="vollstaendig">false, wenn mindestens ein Bild nicht geladen werden konnte.</param>
        private Bitmap GeneriereBild(FileInfo[] dateien, int dpi, out bool vollstaendig)
        {
            vollstaendig = true;
            double groesseCm = _groesse;
            int groesseInPixel = InPixel(groesseCm, dpi);
            Bitmap neuesBild = new Bitmap(InPixel(SeitenLayout.SeitenBreiteCm, dpi), InPixel(SeitenLayout.SeitenHoeheCm, dpi));
            neuesBild.SetResolution(dpi, dpi);
            var layout = new SeitenLayout(groesseCm);
            using (Graphics g = Graphics.FromImage(neuesBild)) {
                // Weißer Hintergrund, da JPEG keine Transparenz kennt
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Für den Druck hochwertig skalieren, für die Vorschau genügt die schnellere Variante
                if (dpi >= DruckDpi)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                }
                for (int i = 0; i < dateien.Length; i++)
                {
                    using Image? image = BildLader.LadeBild(dateien[i].FullName);
                    if (image == null)
                    {
                        vollstaendig = false;
                        continue;
                    }
                    BildInfo? info = BildInfoSpeicher.Lade(dateien[i].FullName);
                    Ausschnitt ausschnitt = AusschnittRechner.Waehle(info, image.Width, image.Height, groesseCm);
                    // Position in cm berechnen und erst dann umrechnen, damit sich Rundungsfehler nicht aufsummieren
                    RectangleF kreisCm = layout.KreisRechteckCm(i);
                    int x = InPixel(kreisCm.X, dpi);
                    int y = InPixel(kreisCm.Y, dpi);
                    var zielRechteck = new Rectangle(x, y, groesseInPixel, groesseInPixel);
                    ZeichneKreisausschnitt(g, image, ausschnitt, zielRechteck);
                    // Markierung nur am Bildschirm, nie auf dem Ausdruck
                    if (dpi < DruckDpi && info?.Manuell != null)
                    {
                        ZeichneStift(g, zielRechteck, dpi);
                    }
                }
            };

            return neuesBild;
        }

        private async Task DruckeBildAsync(Image bild) {
            string drucker = _comboBox.Text;
            try
            {
                // Das Übergeben an den Druckerspooler dauert und würde sonst die Oberfläche samt Druckanzeige einfrieren
                await Task.Run(() => Drucke(bild, drucker));
            }
            catch (InvalidPrinterException) {
                MessageBox.Show("Der angegebene Drucker ist nicht erreichbar. Die Datei liegt im entsprechenden Ausgabeordner und kann manuell gedruckt werden.", "Fehler beim Drucken", MessageBoxButtons.OK);
            }
            catch (Exception)
            {
                MessageBox.Show("Es ist ein Fehler beim Drucken aufgetreten. Die Datei liegt im entsprechenden Ausgabeordner und kann manuell gedruckt werden.", "Fehler beim Drucken", MessageBoxButtons.OK);
            }
        }

        private static void Drucke(Image bild, string drucker)
        {
            using var printDocument = new PrintDocument();
            printDocument.PrinterSettings.PrinterName = drucker;
            printDocument.DefaultPageSettings.Landscape = false;
            printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            printDocument.PrintPage += (sender, args) =>
            {
                if (args.Graphics == null) return;
                // Beim Drucker entspricht GraphicsUnit.Display 1/100 Zoll; das Bild wird in seiner physischen Größe platziert
                args.Graphics.PageUnit = GraphicsUnit.Display;
                args.Graphics.PageScale = 1;
                args.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                float breite = bild.Width / bild.HorizontalResolution * 100f;
                float hoehe = bild.Height / bild.VerticalResolution * 100f;
                args.Graphics.DrawImage(bild, -args.PageSettings.HardMarginX, -args.PageSettings.HardMarginY, breite, hoehe);
            };
            printDocument.Print();
        }

        /// <summary>
        /// Schneidet den quadratischen Ausschnitt aus dem Bild, skaliert ihn direkt auf die Zielgröße und zeichnet ihn
        /// kreisförmig. Es entsteht kein Zwischenbild in voller Auflösung.
        /// </summary>
        private static void ZeichneKreisausschnitt(Graphics ziel, Image p_Bild, Ausschnitt ausschnitt, Rectangle zielRechteck)
        {
            if (zielRechteck.Width <= 0) return;

            RectangleF bildausschnitt = AusschnittRechner.InPixel(ausschnitt, p_Bild.Width, p_Bild.Height);
            using Bitmap skaliert = new Bitmap(zielRechteck.Width, zielRechteck.Height);
            skaliert.SetResolution(ziel.DpiX, ziel.DpiY);
            using (Graphics g = Graphics.FromImage(skaliert))
            {
                g.InterpolationMode = ziel.InterpolationMode;
                g.PixelOffsetMode = ziel.PixelOffsetMode;
                g.DrawImage(p_Bild, new Rectangle(0, 0, zielRechteck.Width, zielRechteck.Height), bildausschnitt, GraphicsUnit.Pixel);
            }

            using TextureBrush brush = new TextureBrush(skaliert);
            brush.TranslateTransform(zielRechteck.X, zielRechteck.Y);
            ziel.FillEllipse(brush, zielRechteck);
        }

        /// <summary>
        /// Kennzeichnet in der Vorschau, dass der Ausschnitt von Hand korrigiert wurde.
        /// </summary>
        private static void ZeichneStift(Graphics ziel, Rectangle kreis, int dpi)
        {
            int durchmesser = InPixel(0.8, dpi); // die Vorschau wird verkleinert angezeigt, kleiner wäre das Symbol nicht mehr erkennbar
            var punkt = new Rectangle(kreis.Right - durchmesser, kreis.Bottom - durchmesser, durchmesser, durchmesser);
            using var hintergrund = new SolidBrush(Color.FromArgb(0, 120, 212));
            ziel.FillEllipse(hintergrund, punkt);
            using var schrift = new Font("Segoe UI Symbol", durchmesser * 0.55f, GraphicsUnit.Pixel);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            ziel.DrawString("✎", schrift, Brushes.White, punkt, format);
        }

        private static int InPixel(double cm, int dpi)
        {
            return (int)Math.Round(cm / cmProInch * dpi);
        }
    }
}
