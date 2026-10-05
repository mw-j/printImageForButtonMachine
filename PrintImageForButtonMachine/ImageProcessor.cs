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
        private const double SeitenBreiteCm = 21; // A4
        private const double SeitenHoeheCm = 29.7;
        private const double AbstandCm = 0.5;
        private const int VorschauDpi = 96; // genügt für die Bildschirmvorschau
        private const int DruckDpi = 300; // mehr bringt auch bei 600-dpi-Laserdruckern keinen sichtbaren Gewinn
        private TextBox _ueberwachungPfad;
        private TextBox _ausgabePfad;
        private double _faktor; // [0,1]
        private double _groesse; // cm
        private PictureBox _pictureBox;
        private Label _labelAktualisiert;
        private readonly ComboBox _comboBox;
        private string? _vorschauSignatur; // Stand der zuletzt erzeugten Vorschau
        private bool _beschaeftigt; // verhindert parallele Verarbeitung (nur im UI-Thread verwendet)

        public string UeberwachungPfad { get => _ueberwachungPfad.Text; set => _ueberwachungPfad.Text = value; }
        public string AusgabePfad { get => _ausgabePfad.Text; set => _ausgabePfad.Text = value; }
        public double Faktor { get => _faktor * 100; set => _faktor = value / 100; }
        public double Groesse { get => _groesse; set => _groesse = value; }

        public ImageProcessor(TextBox p_UeberwachungPfad, TextBox p_ausgabePfad, double p_Faktor, double p_Groesse, PictureBox p_PictureBox, Label labelAktualisiert, ComboBox comboBox)
        {
            _ueberwachungPfad = p_UeberwachungPfad;
            _ausgabePfad = p_ausgabePfad;
            _faktor = p_Faktor / 100;
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
            if (dateien.Length > 0 && dateien.Length >= ErmittleAnzahlBilderProSeite())
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
            if (_beschaeftigt) return;
            _beschaeftigt = true;
            try
            {
                FileInfo[] files = LeseBilder(UeberwachungPfad);
                if (files.Length == 0) return;

                // verschiebe Dateien in eigenen Ordner
                string ordnerName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string ordnerPfad = Path.Combine(AusgabePfad, ordnerName);
                var verschobeneDateien = new List<(string Quelle, string Ziel)>();
                try
                {
                    Directory.CreateDirectory(ordnerPfad);
                    foreach (FileInfo file in files)
                    {
                        string quelle = file.FullName;
                        string ziel = Path.Combine(ordnerPfad, file.Name);
                        file.MoveTo(ziel);
                        verschobeneDateien.Add((quelle, ziel));
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show("Eine Datei im überwachten Ordner konnte nicht kopiert werden, da sie von einem anderen Programm blockiert wird. Möglicherweise hat das Aufnahmeprogramm noch eine Vorschau geöffnet. ", "Bearbeitung nicht möglich", MessageBoxButtons.OK);
                    // Verschiebe bereits verschobene Fotos zurück
                    foreach (var (quelle, ziel) in verschobeneDateien)
                    {
                        File.Move(ziel, quelle);
                    }
                    if (Directory.Exists(ordnerPfad) && !Directory.EnumerateFileSystemEntries(ordnerPfad).Any())
                    {
                        Directory.Delete(ordnerPfad);
                    }
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
                DruckeBild(neuesBild);
                _vorschauSignatur = null;
            }
            finally
            {
                _beschaeftigt = false;
            }
        }

        /// <param name="erzwingen">Vorschau auch dann neu erzeugen, wenn sich nichts geändert hat.</param>
        public async Task GeneriereVorschauAsync(bool erzwingen)
        {
            if (_beschaeftigt) return;
            _beschaeftigt = true;
            try
            {
                FileInfo[] dateien = LeseBilder(UeberwachungPfad);
                string signatur = ErzeugeSignatur(dateien);
                if (erzwingen || signatur != _vorschauSignatur)
                {
                    bool vollstaendig = false;
                    Bitmap vorschaubild = await Task.Run(() => GeneriereBild(dateien, VorschauDpi, out vollstaendig));
                    Image? altesBild = _pictureBox.Image;
                    _pictureBox.Image = vorschaubild;
                    altesBild?.Dispose();
                    // Nicht ladbare Bilder (z. B. noch im Schreibvorgang) beim nächsten Durchlauf erneut versuchen
                    _vorschauSignatur = vollstaendig ? signatur : null;
                }
                _labelAktualisiert.Text = $"Zuletzt aktualisiert: {DateTime.Now.ToString("HH:mm:ss")}";
            }
            finally
            {
                _beschaeftigt = false;
            }
        }

        public int ErmittleAnzahlBilderProSeite()
        {
            int anzahlSpalten = ErmittleAnzahlBilder(SeitenBreiteCm);
            int anzahlZeilen = ErmittleAnzahlBilder(SeitenHoeheCm);
            return anzahlSpalten * anzahlZeilen;
        }

        private static FileInfo[] LeseBilder(string ordnerPfad)
        {
            return new DirectoryInfo(ordnerPfad).GetFiles("*.jpg");
        }

        private string ErzeugeSignatur(FileInfo[] dateien)
        {
            var signatur = new StringBuilder();
            signatur.Append(_faktor).Append('|').Append(_groesse);
            foreach (FileInfo datei in dateien)
            {
                signatur.Append('|').Append(datei.Name).Append(':').Append(datei.Length).Append(':').Append(datei.LastWriteTimeUtc.Ticks);
            }
            return signatur.ToString();
        }

        /// <param name="vollstaendig">false, wenn mindestens ein Bild nicht geladen werden konnte.</param>
        private Bitmap GeneriereBild(FileInfo[] dateien, int dpi, out bool vollstaendig)
        {
            vollstaendig = true;
            double groesseCm = _groesse;
            int groesseInPixel = InPixel(groesseCm, dpi);
            Bitmap neuesBild = new Bitmap(InPixel(SeitenBreiteCm, dpi), InPixel(SeitenHoeheCm, dpi));
            neuesBild.SetResolution(dpi, dpi);
            int anzahlBilderProZeile = ErmittleAnzahlBilder(SeitenBreiteCm);
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
                    using Image? image = LadeBild(dateien[i].FullName);
                    if (image == null)
                    {
                        vollstaendig = false;
                        continue;
                    }
                    // Position in cm berechnen und erst dann umrechnen, damit sich Rundungsfehler nicht aufsummieren
                    int x = InPixel(AbstandCm + (i % anzahlBilderProZeile) * (groesseCm + AbstandCm), dpi);
                    int y = InPixel(AbstandCm + (i / anzahlBilderProZeile) * (groesseCm + AbstandCm), dpi);
                    ZeichneKreisausschnitt(g, image, new Rectangle(x, y, groesseInPixel, groesseInPixel));
                }
            };

            return neuesBild;
        }

        /// <summary>
        /// Lädt das Bild vollständig in den Speicher, damit die Datei nicht für die Dauer der Verarbeitung gesperrt bleibt.
        /// Liefert null, wenn die Datei (noch) nicht gelesen werden kann.
        /// </summary>
        private static Image? LadeBild(string pfad)
        {
            try
            {
                // Der MemoryStream muss so lange leben wie das Bild; er hält keine nativen Ressourcen
                return Image.FromStream(new MemoryStream(File.ReadAllBytes(pfad)));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is OutOfMemoryException)
            {
                return null;
            }
        }

        private void DruckeBild(Image bild) {
            try
            {
                using var printDocument = new PrintDocument();
                printDocument.PrinterSettings.PrinterName = _comboBox.Text;
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
            catch (InvalidPrinterException) {
                MessageBox.Show("Der angegebene Drucker ist nicht erreichbar. Die Datei liegt im entsprechenden Ausgabeordner und kann manuell gedruckt werden.", "Fehler beim Drucken", MessageBoxButtons.OK);
            }
            catch (Exception)
            {
                MessageBox.Show("Es ist ein Fehler beim Drucken aufgetreten. Die Datei liegt im entsprechenden Ausgabeordner und kann manuell gedruckt werden.", "Fehler beim Drucken", MessageBoxButtons.OK);
            }

        }

        /// <summary>
        /// Schneidet einen quadratischen Ausschnitt (Anteil <see cref="_faktor"/> der kürzeren Kante) aus der Bildmitte,
        /// skaliert ihn direkt auf die Zielgröße und zeichnet ihn kreisförmig. Es entsteht kein Zwischenbild in voller Auflösung.
        /// </summary>
        private void ZeichneKreisausschnitt(Graphics ziel, Image p_Bild, Rectangle zielRechteck)
        {
            if (_faktor < 0 || _faktor > 1) throw new ArgumentOutOfRangeException(nameof(Faktor));

            int kantenlaenge = (int)(Math.Min(p_Bild.Width, p_Bild.Height) * _faktor);
            if (kantenlaenge <= 0 || zielRechteck.Width <= 0) return;

            Rectangle bildausschnitt = new Rectangle((p_Bild.Width - kantenlaenge) / 2, (p_Bild.Height - kantenlaenge) / 2, kantenlaenge, kantenlaenge);
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

        private int ErmittleAnzahlBilder(double p_verfügbareLängeCm)
        {
            return (int)((p_verfügbareLängeCm - AbstandCm) / (AbstandCm + _groesse));
        }

        private static int InPixel(double cm, int dpi)
        {
            return (int)Math.Round(cm / cmProInch * dpi);
        }
    }
}
