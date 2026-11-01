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

        private void pictureBox1_Paint(object? sender, PaintEventArgs e)
        {
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