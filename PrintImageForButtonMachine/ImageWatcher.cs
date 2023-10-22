using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    internal class ImageWatcher
    {
        private FileSystemWatcher watcher;
        private PrintDocument printDocument;
        private string printPath;
        private readonly string _ueberwachungPfad;
        private readonly string _ausgabePfad;
        private readonly double _faktor; // [0,1]
        private readonly double _groesse;
        private int _abstand = 60; // Zwischen Bildern und zum Rand; in Pixel

        public ImageWatcher(string p_UeberwachungPfad, string p_ausgabePfad, double p_Faktor, double p_Groesse)
        {
            _ueberwachungPfad = p_UeberwachungPfad;
            _ausgabePfad = p_ausgabePfad;
            _faktor = p_Faktor / 100;
            _groesse = p_Groesse;

            if (string.IsNullOrWhiteSpace(_ueberwachungPfad)) {
                MessageBox.Show("Es wurde kein Ordner zum Überwachen angegeben", "Unvollständige Angaben", MessageBoxButtons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(_ausgabePfad))
            {
                MessageBox.Show("Es wurde kein Ausgabeordner angegeben", "Unvollständige Angaben", MessageBoxButtons.OK);
                return;
            }

            if (_ausgabePfad == _ueberwachungPfad)
            {
                MessageBox.Show("Überwachter Ordner und Ausgabeordner müssen unterschiedliche sein", "Unterschiedliche Ordner auswählen", MessageBoxButtons.OK);
                return;
            }

            watcher = new FileSystemWatcher();
            watcher.Path = _ueberwachungPfad;
            watcher.Filter = "*.jpg";
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

            printDocument = new PrintDocument();
            printDocument.PrintPage += new PrintPageEventHandler(PrintPage);

        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            DirectoryInfo directory = new DirectoryInfo(watcher.Path);
            FileInfo[] files = directory.GetFiles("*.jpg");

            double pixelProCM = 118.11;
            int groesseInPixel = (int) Math.Floor(pixelProCM * _groesse);
            int a4Breite = 2480; // in Pixel bei 300dpi
            int a4Höhe = 3508; // in Pixel bei 300dp
            int anzahlBilderProZeile = ErmittleAnzahlBilder(2480, groesseInPixel);
            int anzahlBilderProSpalte = ErmittleAnzahlBilder(3508, groesseInPixel);
            int anzahlBilderProSeite = anzahlBilderProZeile * anzahlBilderProSpalte;

            if (files.Length >= 6)
            {
                Bitmap a4Blatt = new Bitmap(a4Breite, a4Höhe);
                Graphics g = Graphics.FromImage(a4Blatt);
                for (int i = 0; i < 6; i++)
                {
                    Image image = Image.FromFile(files[i].FullName);
                    Image imageZugeschnitten = ZuschneidenBild(image, _faktor);
                    var imageSkaliert = SkaliereBild(imageZugeschnitten, groesseInPixel);
                    //int x = (i % 2) * (a4Blatt.Width / 2) + (a4Blatt.Width / 4) - (imageSkaliert.Width / 2); // Position images in two columns
                    //int y = (i / 2) * (a4Blatt.Height / 3) + (a4Blatt.Height / 6) - (imageSkaliert.Height / 2); // Position images in three rows
                    int x = _abstand + (i % anzahlBilderProZeile) * (groesseInPixel + _abstand);
                    int y = _abstand + (int)Math.Floor((double)(i / anzahlBilderProZeile)) * (groesseInPixel + _abstand);
                    g.DrawImage(imageSkaliert, new Rectangle(x, y, imageSkaliert.Width, imageSkaliert.Height));
                }
                printPath = Path.Combine(watcher.Path, "print.jpg");
                a4Blatt.Save(printPath);
                printDocument.Print(); // Open print dialog
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Image image = Image.FromFile(printPath);
            e.Graphics.DrawImage(image, e.MarginBounds.Left, e.MarginBounds.Top);
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

        private int ErmittleAnzahlBilder(int p_verfügbareLänge, int p_LängeProBild) {
            
            return (p_verfügbareLänge - _abstand) / (_abstand + p_LängeProBild);
        }
    }
}
