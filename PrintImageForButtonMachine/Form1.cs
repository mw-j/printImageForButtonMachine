namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btn_startStop_Click(object sender, EventArgs e)
        {
            new ImageWatcher(textBox_ueberwachung.Text, textBox_ausgabe.Text, (double)num_faktor.Value, (double)num_groesse.Value);
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
    }
}