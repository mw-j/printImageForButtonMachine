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

        public Form1()
        {
            InitializeComponent();
            processor = new ImageProcessor(textBox_ueberwachung, textBox_ausgabe, (double)num_faktor.Value, (double)num_groesse.Value, pictureBox1, label_Uhrzeit, comboBox_drucker);
            checker = new ImageChecker(processor);
            checker.Gestoppt += (sender, e) => DeAktiviereInputs(true);
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