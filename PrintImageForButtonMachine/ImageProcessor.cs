using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintImageForButtonMachine
{
    internal class ImageProcessor
    {
        private string _printPath;
        private string _ueberwachungPfad;
        private string _ausgabePfad;
        private double _faktor; // [0,1]
        private double _groesse; // cm
        private PictureBox _pictureBox;
        private int _druckerAuflösung = 96; // dpi
        private Image _vorschauBild = new Bitmap(20, 20);
        private Label _labelAktualisiert;

        public string UeberwachungPfad { get => _ueberwachungPfad; set => _ueberwachungPfad = value; }
        public string AusgabePfad { get => _ausgabePfad; set => _ausgabePfad = value; }
        public double Faktor { get => _faktor * 100; set => _faktor = value / 100; }
        public double Groesse { get => _groesse; set => _groesse = value; }
        
        public int DruckerAuflösung { get => _druckerAuflösung; set => _druckerAuflösung = value; }
        public int BreiteInPixel { get => (int)(21 / 2.54 * _druckerAuflösung); }
        public int HöheInPixel { get => (int)(29.7 / 2.54 * _druckerAuflösung); }
        public int GroesseInPixel { get => (int)(_groesse / 2.54 * _druckerAuflösung); }
        public int AbstandInPixel { get => (int)(0.5 / 2.54 * _druckerAuflösung); }
        public Image VorschauBild { get => _vorschauBild; }

        public ImageProcessor(string p_UeberwachungPfad, string p_ausgabePfad, double p_Faktor, double p_Groesse, PictureBox p_PictureBox, Label labelAktualisiert)
        {
            _ueberwachungPfad = p_UeberwachungPfad;
            _ausgabePfad = p_ausgabePfad;
            _faktor = p_Faktor / 100;
            _groesse = p_Groesse;
            _pictureBox = p_PictureBox;
            _labelAktualisiert = labelAktualisiert;
        }

        public void BearbeiteOrdner()
        {
            DirectoryInfo directory = new DirectoryInfo(_ueberwachungPfad);
            FileInfo[] files = directory.GetFiles("*.jpg");

            // verschiebe Dateien in eigenen Ordner
            string ordnerName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string ordnerPfad = Path.Combine(_ausgabePfad, ordnerName);
            if (!Directory.Exists(ordnerPfad))
            {
                Directory.CreateDirectory(ordnerPfad);
            }
            foreach (FileInfo file in files)
            {
                string pfad = Path.Combine(ordnerPfad, file.Name);
                file.MoveTo(pfad);
            }

            using (Bitmap neuesBild = GeneriereBild(ordnerPfad)) {
                // Speichere generiertes Bild
                _printPath = Path.Combine(ordnerPfad, "print.jpg");
                if (!File.Exists(_printPath))
                {
                    File.Delete(_printPath);
                }
                neuesBild.Save(_printPath);
            } ;
            DruckeBild();
        }

        public void GeneriereVorschau() {
            Image vorschaubild = GeneriereBild(_ueberwachungPfad);
            _pictureBox.Image = vorschaubild;
            _labelAktualisiert.Text = $"Zuletzt aktualisiert: {DateTime.Now.ToString("HH:mm:ss")}";
        }

        public int ErmittleAnzahlBilderProSeite()
        {
            int anzahlSpalten = ErmittleAnzahlBilder(BreiteInPixel, GroesseInPixel);
            int anzahlZeilen = ErmittleAnzahlBilder(HöheInPixel, GroesseInPixel);
            return anzahlSpalten * anzahlZeilen;
        }

        private Bitmap GeneriereBild(string ordnerPfad)
        {
            DirectoryInfo neuesVerzeichnis = new DirectoryInfo(ordnerPfad);
            FileInfo[] dateien = neuesVerzeichnis.GetFiles("*.jpg");
            Bitmap neuesBild = new Bitmap(BreiteInPixel, HöheInPixel);
            int anzahlBilderProZeile = ErmittleAnzahlBilder(BreiteInPixel, GroesseInPixel);
            using (Graphics g = Graphics.FromImage(neuesBild)) {
                for (int i = 0; i < dateien.Length; i++)
                {
                    using (Image image = Image.FromFile(dateien[i].FullName)) {
                        Image imageZugeschnitten = ZuschneidenBild(image, _faktor);
                        var imageSkaliert = SkaliereBild(imageZugeschnitten, GroesseInPixel);
                        int x = AbstandInPixel + (i % anzahlBilderProZeile) * (GroesseInPixel + AbstandInPixel);
                        int y = AbstandInPixel + (int)Math.Floor((double)(i / anzahlBilderProZeile)) * (GroesseInPixel + AbstandInPixel);
                        g.DrawImage(imageSkaliert, new Rectangle(x, y, imageSkaliert.Width, imageSkaliert.Height));
                    } ;
                }
            };
                    
            return neuesBild;
        }

        private void DruckeBild() {
            var printDocument = new PrintDocument();
            printDocument.DefaultPageSettings.Landscape = false;
            printDocument.PrintPage += (sender, args) =>
            {
                Image image = Image.FromFile(_printPath);
                args.Graphics.DrawImage(image, 0, 0, image.Width, image.Height);
            };
            printDocument.Print();
        }

        private Image ZuschneidenBild(Image p_Bild, double p_Faktor)
        {
            if (p_Faktor < 0 || p_Faktor > 1) throw new Exception();

            var kleinsteKantenlaenge = Math.Min(p_Bild.Width, p_Bild.Height);
            int zielgroesse = (int)(kleinsteKantenlaenge * p_Faktor);
            Bitmap bild = new Bitmap(p_Bild);
            Bitmap zugeschnittenesBild = new Bitmap(zielgroesse, zielgroesse);

            // Erstellen Sie ein Graphics-Objekt aus dem zugeschnittenen Bild
            using (Graphics g = Graphics.FromImage(zugeschnittenesBild))
            {
                int x = (p_Bild.Width - zielgroesse) / 2;
                int y = (p_Bild.Height - zielgroesse) / 2;
                Rectangle bildausschnitt = new Rectangle(x, y, zielgroesse, zielgroesse);
                Rectangle kreis = new Rectangle(0, 0, zielgroesse, zielgroesse);
                using (Brush brush = new TextureBrush(bild, bildausschnitt))
                {
                    g.FillEllipse(brush, kreis);
                }
            }
            return zugeschnittenesBild;
        }

        private Image SkaliereBild(Image bild, int maximaleKantenlaenge)
        {
            // Berechnen Sie das Skalierungsverhältnis
            double verhaeltnisX = (double)maximaleKantenlaenge / bild.Width;
            double verhaeltnisY = (double)maximaleKantenlaenge / bild.Height;
            double verhaeltnis = Math.Min(verhaeltnisX, verhaeltnisY);

            // Berechnen Sie die neue Breite und Höhe
            int neueBreite = (int)(bild.Width * verhaeltnis);
            int neueHoehe = (int)(bild.Height * verhaeltnis);

            // Erstellen Sie ein neues Bild mit der neuen Breite und Höhe
            Image neuesBild = new Bitmap(neueBreite, neueHoehe);

            using (Graphics graphics = Graphics.FromImage(neuesBild))
            {
                // Zeichnen Sie das ursprüngliche Bild auf das neue Bild
                graphics.DrawImage(bild, 0, 0, neueBreite, neueHoehe);
            }

            // Geben Sie das skalierte Bild zurück
            return neuesBild;
        }

        private int ErmittleAnzahlBilder(int p_verfügbareLänge, int p_LängeProBild)
        {

            return (p_verfügbareLänge - AbstandInPixel) / (AbstandInPixel + p_LängeProBild);
        }
    }
}
