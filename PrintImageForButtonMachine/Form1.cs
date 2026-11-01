using PrintImageForButtonMachine;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private ImageProcessor processor;
        private ImageChecker checker;
        private readonly ProgressBar _druckFortschritt = new() { Style = ProgressBarStyle.Marquee, Size = new Size(180, 10), Visible = false };
        private int? _druckAnzahl; // gesetzt, solange eine Seite gedruckt wird
        private readonly ToolTip _toolTip = new();
        private int? _hoverIndex; // Kreis unter der Maus

        public Form1()
        {
            InitializeComponent();
            processor = new ImageProcessor(textBox_ueberwachung, textBox_ausgabe, (double)num_faktor.Value, (double)num_groesse.Value, pictureBox1, label_Uhrzeit, comboBox_drucker);
            checker = new ImageChecker(processor);
            checker.Gestoppt += (sender, e) => DeAktiviereInputs(true);
            processor.DruckGestartet += (sender, anzahl) => ZeigeDruck(anzahl);
            processor.DruckBeendet += (sender, e) => ZeigeDruck(null);
            pictureBox1.Controls.Add(_druckFortschritt);
            pictureBox1.Paint += pictureBox1_Paint;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseLeave += (sender, e) => SetzeHover(null);
            pictureBox1.MouseDoubleClick += pictureBox1_MouseDoubleClick;
            textBox_ueberwachung.DataBindings.Add("Text", processor, "UeberwachungPfad");
            textBox_ausgabe.DataBindings.Add("Text", processor, "AusgabePfad");
            num_faktor.DataBindings.Add("Value", processor, "Faktor");
            num_groesse.DataBindings.Add("Value", processor, "Groesse");
            //pictureBox1.DataBindings.Add("Image", processor, "VorschauBild");

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                comboBox_drucker.Items.Add(printer);
            }
            using PrintDocument pd = new PrintDocument();
            comboBox_drucker.Text = pd.PrinterSettings.PrinterName;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btn_startStop_Click(object sender, EventArgs e)
        {
            if (!checker.DoCheck)
            {
                if (checker.startCheck())
                {
                    DeAktiviereInputs(false);
                }
            }
            else
            {
                checker.stopCheck();
                DeAktiviereInputs(true);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            checker.stopCheck();
            base.OnFormClosing(e);
        }

        /// <param name="anzahl">Anzahl der Fotos, die gedruckt werden; null, wenn der Druck beendet ist.</param>
        private void ZeigeDruck(int? anzahl)
        {
            _druckAnzahl = anzahl;
            bool frei = anzahl == null;
            btn_startStop.Enabled = frei;
            btn_print.Enabled = frei;
            button1.Enabled = frei;
            btn_print.Text = frei ? "Jetzt drucken" : "Seite wird gedruckt …";
            UseWaitCursor = !frei;
            _druckFortschritt.Location = new Point((pictureBox1.ClientSize.Width - _druckFortschritt.Width) / 2, pictureBox1.ClientSize.Height / 2);
            _druckFortschritt.Visible = !frei;
            pictureBox1.Invalidate();
        }

        /// <summary>
        /// Maßstab und Versatz, mit denen die Vorschau bei <see cref="PictureBoxSizeMode.Zoom"/> angezeigt wird,
        /// umgerechnet auf Bildschirmpixel je cm.
        /// </summary>
        private (float ProCm, PointF Versatz)? VorschauAbbildung()
        {
            Image? bild = pictureBox1.Image;
            if (bild == null) return null;
            Size flaeche = pictureBox1.ClientSize;
            float skala = Math.Min((float)flaeche.Width / bild.Width, (float)flaeche.Height / bild.Height);
            var versatz = new PointF((flaeche.Width - bild.Width * skala) / 2, (flaeche.Height - bild.Height * skala) / 2);
            return (skala * bild.HorizontalResolution / 2.54f, versatz);
        }

        private int? KreisIndexAn(Point punkt)
        {
            if (VorschauAbbildung() is not var (proCm, versatz)) return null;
            double xCm = (punkt.X - versatz.X) / proCm;
            double yCm = (punkt.Y - versatz.Y) / proCm;
            return new SeitenLayout(processor.GroesseCm).IndexAn(xCm, yCm, processor.VorschauDateien.Length);
        }

        private void SetzeHover(int? index)
        {
            if (index == _hoverIndex) return;
            _hoverIndex = index;
            pictureBox1.Cursor = index != null ? Cursors.Hand : Cursors.Default;
            _toolTip.SetToolTip(pictureBox1, index != null ? "Doppelklick: Ausschnitt bearbeiten" : null);
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseMove(object? sender, MouseEventArgs e)
        {
            SetzeHover(_druckAnzahl == null ? KreisIndexAn(e.Location) : null);
        }

        private async void pictureBox1_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_druckAnzahl != null || KreisIndexAn(e.Location) is not int index) return;
            try
            {
                FileInfo foto = processor.VorschauDateien[index];
                using Image? bild = BildLader.LadeBild(foto.FullName);
                if (bild == null) return;
                BildInfo? info = BildInfoSpeicher.Lade(foto.FullName);
                List<Gesicht> gesichter = info?.Gesichter ?? [];
                Ausschnitt automatisch = AusschnittRechner.Automatisch(bild.Width, bild.Height, gesichter, processor.FaktorAnteil, processor.GroesseCm);
                Ausschnitt start = AusschnittRechner.Waehle(info, bild.Width, bild.Height, processor.FaktorAnteil, processor.GroesseCm);
                SetzeHover(null);
                processor.BearbeitungOffen = true;
                try
                {
                    using var form = new AusschnittForm(bild, foto.Name, start, automatisch, processor.GroesseCm, gesichter.Count);
                    if (form.ShowDialog(this) != DialogResult.OK) return;
                    // Wurde das Foto inzwischen gedruckt oder gelöscht, wird nichts gespeichert
                    BildInfoSpeicher.Speichere(foto.FullName, (info ?? new BildInfo([], null)) with { Manuell = form.Ergebnis });
                }
                finally
                {
                    processor.BearbeitungOffen = false;
                }
                await processor.GeneriereVorschauAsync(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Der Ausschnitt konnte nicht bearbeitet werden:\n{ex.Message}", "Fehler", MessageBoxButtons.OK);
            }
        }

        private void pictureBox1_Paint(object? sender, PaintEventArgs e)
        {
            if (_druckAnzahl == null && _hoverIndex is int hover && VorschauAbbildung() is var (proCm, versatz))
            {
                RectangleF kreisCm = new SeitenLayout(processor.GroesseCm).KreisRechteckCm(hover);
                var kreis = new RectangleF(versatz.X + kreisCm.X * proCm, versatz.Y + kreisCm.Y * proCm, kreisCm.Width * proCm, kreisCm.Height * proCm);
                kreis.Inflate(3, 3);
                using var rahmen = new Pen(Color.FromArgb(0, 120, 212), 2);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawEllipse(rahmen, kreis);
            }
            if (_druckAnzahl is not int anzahl) return;
            Rectangle flaeche = pictureBox1.ClientRectangle;
            using var schleier = new SolidBrush(Color.FromArgb(210, Color.White));
            e.Graphics.FillRectangle(schleier, flaeche);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            int mitte = flaeche.Height / 2;
            using var titel = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            e.Graphics.DrawString("Seite wird gedruckt …", titel, Brushes.Black, new RectangleF(0, mitte - 40, flaeche.Width, 30), format);
            e.Graphics.DrawString($"{anzahl} Bilder werden aufbereitet", Font, Brushes.Black, new RectangleF(0, mitte + 15, flaeche.Width, 25), format);
        }

        private void DeAktiviereInputs(bool wert)
        {
            textBox_ueberwachung.Enabled = wert;
            btn_ueberwachung.Enabled = wert;
            textBox_ausgabe.Enabled = wert;
            btn_ausgabe.Enabled = wert;
            num_faktor.Enabled = wert;
            num_groesse.Enabled = wert;
            label_aktivIcon.Visible = !wert;
            label_aktivText.Visible = !wert;
            btn_startStop.Text = wert ? "Überwachung starten" : "Überwachung stoppen";
            comboBox_drucker.Enabled = wert;
        }

        private void textBox_ueberwachung_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btn_ueberwachung_Click(object sender, EventArgs e)
        {
            // Erstellen Sie eine neue Instanz von FolderBrowserDialog
            using FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                // Der Benutzer hat einen Ordner ausgewählt und auf "OK" geklickt
                // Der Pfad zum Ordner ist in folderBrowserDialog1.SelectedPath
                // Sie können diesen Pfad nun verwenden
                string path = folderBrowserDialog1.SelectedPath;
                textBox_ueberwachung.Text = path;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click_1(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void btn_ausgabe_Click(object sender, EventArgs e)
        {
            // Erstellen Sie eine neue Instanz von FolderBrowserDialog
            using FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                // Der Benutzer hat einen Ordner ausgewählt und auf "OK" geklickt
                // Der Pfad zum Ordner ist in folderBrowserDialog1.SelectedPath
                // Sie können diesen Pfad nun verwenden
                string path = folderBrowserDialog1.SelectedPath;
                textBox_ausgabe.Text = path;
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private async void btn_print_Click(object sender, EventArgs e)
        {
            try
            {
                await processor.BearbeiteOrdnerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Die Bilder konnten nicht verarbeitet werden:\n{ex.Message}", "Fehler", MessageBoxButtons.OK);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                await processor.GeneriereVorschauAsync(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Die Vorschau konnte nicht erzeugt werden:\n{ex.Message}", "Fehler", MessageBoxButtons.OK);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click_1(object sender, EventArgs e)
        {

        }
    }
}