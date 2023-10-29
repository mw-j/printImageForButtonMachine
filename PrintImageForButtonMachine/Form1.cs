using PrintImageForButtonMachine;
using System.Diagnostics;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private ImageProcessor processor;
        private ImageWatcher watcher;

        public Form1()
        {
            InitializeComponent();
            processor = new ImageProcessor(textBox_ueberwachung.Text, textBox_ausgabe.Text, (double)num_faktor.Value, (double)num_groesse.Value, pictureBox1);
            watcher = new ImageWatcher(processor);
            textBox_ueberwachung.DataBindings.Add("Text", processor, "UeberwachungPfad");
            textBox_ausgabe.DataBindings.Add("Text", processor, "AusgabePfad");
            num_faktor.DataBindings.Add("Value", processor, "Faktor");
            num_groesse.DataBindings.Add("Value", processor, "Groesse");
            //pictureBox1.DataBindings.Add("Image", processor, "VorschauBild");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btn_startStop_Click(object sender, EventArgs e)
        {
            if (watcher.Watcher.EnableRaisingEvents == false)
            {
                // Watcher starten
                watcher.StarteFileSystemWatcher();
                processor.GeneriereVorschau();
                DeAktiviereInputs(false);

            }
            else
            {
                // Watcher beenden
                watcher.StoppeFileSystemWatcher();
                DeAktiviereInputs(true);
            }

        }

        private void DeAktiviereInputs(bool wert)
        {
            textBox_ueberwachung.Enabled = wert;
            btn_ueberwachung.Enabled = wert;
            textBox_ausgabe.Enabled = wert;
            btn_ausgabe.Enabled = wert;
            num_faktor.Enabled = wert;
            num_groesse.Enabled = wert;
            num_Auflösung.Enabled = wert;
            label_aktivIcon.Visible = !wert;
            label_aktivText.Visible = !wert;
            btn_startStop.Text = wert ? "Überwachung starten" : "Überwachung stoppen";
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
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();

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
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();

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

        private void btn_print_Click(object sender, EventArgs e)
        {
            processor.BearbeiteOrdner();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            processor.GeneriereVorschau();
        }
    }
}