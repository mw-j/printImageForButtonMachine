namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            btn_startStop = new Button();
            colorDialog1 = new ColorDialog();
            textBox_ueberwachung = new TextBox();
            label1 = new Label();
            btn_ueberwachung = new Button();
            btn_ausgabe = new Button();
            label2 = new Label();
            textBox_ausgabe = new TextBox();
            btn_print = new Button();
            textBox3 = new TextBox();
            label3 = new Label();
            num_faktor = new NumericUpDown();
            num_groesse = new NumericUpDown();
            label4 = new Label();
            label5 = new Label();
            label_aktivIcon = new Label();
            label_aktivText = new Label();
            label6 = new Label();
            ((System.ComponentModel.ISupportInitialize)num_faktor).BeginInit();
            ((System.ComponentModel.ISupportInitialize)num_groesse).BeginInit();
            SuspendLayout();
            // 
            // btn_startStop
            // 
            btn_startStop.Location = new Point(12, 331);
            btn_startStop.Name = "btn_startStop";
            btn_startStop.Size = new Size(168, 23);
            btn_startStop.TabIndex = 0;
            btn_startStop.Text = "Überwachung starten";
            btn_startStop.UseVisualStyleBackColor = true;
            btn_startStop.Click += btn_startStop_Click;
            // 
            // textBox_ueberwachung
            // 
            textBox_ueberwachung.Location = new Point(27, 132);
            textBox_ueberwachung.Name = "textBox_ueberwachung";
            textBox_ueberwachung.Size = new Size(331, 23);
            textBox_ueberwachung.TabIndex = 1;
            textBox_ueberwachung.Text = "bild-watch";
            textBox_ueberwachung.TextChanged += textBox_ueberwachung_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 114);
            label1.Name = "label1";
            label1.Size = new Size(257, 15);
            label1.TabIndex = 2;
            label1.Text = "1. Wähle den Ordner der überwacht werden soll";
            label1.Click += label1_Click;
            // 
            // btn_ueberwachung
            // 
            btn_ueberwachung.Location = new Point(364, 132);
            btn_ueberwachung.Name = "btn_ueberwachung";
            btn_ueberwachung.Size = new Size(90, 23);
            btn_ueberwachung.TabIndex = 3;
            btn_ueberwachung.Text = "Durchsuchen";
            btn_ueberwachung.UseVisualStyleBackColor = true;
            btn_ueberwachung.Click += btn_ueberwachung_Click;
            // 
            // btn_ausgabe
            // 
            btn_ausgabe.Location = new Point(364, 182);
            btn_ausgabe.Name = "btn_ausgabe";
            btn_ausgabe.Size = new Size(90, 23);
            btn_ausgabe.TabIndex = 6;
            btn_ausgabe.Text = "Durchsuchen";
            btn_ausgabe.UseVisualStyleBackColor = true;
            btn_ausgabe.Click += btn_ausgabe_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 164);
            label2.Name = "label2";
            label2.Size = new Size(159, 15);
            label2.TabIndex = 5;
            label2.Text = "2. Wähle den Ausgabeordner";
            label2.Click += label2_Click;
            // 
            // textBox_ausgabe
            // 
            textBox_ausgabe.Location = new Point(27, 182);
            textBox_ausgabe.Name = "textBox_ausgabe";
            textBox_ausgabe.Size = new Size(331, 23);
            textBox_ausgabe.TabIndex = 4;
            textBox_ausgabe.Text = "bild-out";
            // 
            // btn_print
            // 
            btn_print.Location = new Point(12, 360);
            btn_print.Name = "btn_print";
            btn_print.Size = new Size(168, 23);
            btn_print.TabIndex = 7;
            btn_print.Text = "Jetzt drucken";
            btn_print.UseVisualStyleBackColor = true;
            // 
            // textBox3
            // 
            textBox3.BackColor = SystemColors.Control;
            textBox3.BorderStyle = BorderStyle.None;
            textBox3.ForeColor = SystemColors.ControlText;
            textBox3.Location = new Point(12, 12);
            textBox3.Multiline = true;
            textBox3.Name = "textBox3";
            textBox3.ReadOnly = true;
            textBox3.Size = new Size(442, 82);
            textBox3.TabIndex = 9;
            textBox3.Text = resources.GetString("textBox3.Text");
            textBox3.TextChanged += textBox3_TextChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 221);
            label3.Name = "label3";
            label3.Size = new Size(154, 15);
            label3.TabIndex = 11;
            label3.Text = "3. Größe des Bildausschnitts";
            label3.Click += label3_Click_1;
            // 
            // num_faktor
            // 
            num_faktor.Location = new Point(174, 219);
            num_faktor.Maximum = new decimal(new int[] { 99, 0, 0, 0 });
            num_faktor.Name = "num_faktor";
            num_faktor.Size = new Size(51, 23);
            num_faktor.TabIndex = 12;
            num_faktor.Value = new decimal(new int[] { 80, 0, 0, 0 });
            // 
            // num_groesse
            // 
            num_groesse.DecimalPlaces = 2;
            num_groesse.Location = new Point(159, 248);
            num_groesse.Maximum = new decimal(new int[] { 999, 0, 0, 131072 });
            num_groesse.Name = "num_groesse";
            num_groesse.Size = new Size(66, 23);
            num_groesse.TabIndex = 14;
            num_groesse.Value = new decimal(new int[] { 58, 0, 0, 65536 });
            num_groesse.ValueChanged += numericUpDown2_ValueChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 250);
            label4.Name = "label4";
            label4.Size = new Size(141, 15);
            label4.TabIndex = 13;
            label4.Text = "4. Durchmesser der Bilder";
            label4.Click += label4_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = SystemColors.Window;
            label5.Location = new Point(182, 250);
            label5.Name = "label5";
            label5.Size = new Size(24, 15);
            label5.TabIndex = 15;
            label5.Text = "cm";
            label5.Click += label5_Click;
            // 
            // label_aktivIcon
            // 
            label_aktivIcon.AutoSize = true;
            label_aktivIcon.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label_aktivIcon.ForeColor = Color.FromArgb(0, 192, 0);
            label_aktivIcon.Location = new Point(186, 330);
            label_aktivIcon.Name = "label_aktivIcon";
            label_aktivIcon.Size = new Size(20, 21);
            label_aktivIcon.TabIndex = 16;
            label_aktivIcon.Text = "●";
            label_aktivIcon.Visible = false;
            // 
            // label_aktivText
            // 
            label_aktivText.AutoSize = true;
            label_aktivText.Location = new Point(204, 335);
            label_aktivText.Name = "label_aktivText";
            label_aktivText.Size = new Size(109, 15);
            label_aktivText.TabIndex = 17;
            label_aktivText.Text = "Überwachung aktiv";
            label_aktivText.Visible = false;
            label_aktivText.Click += label7_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BackColor = SystemColors.Window;
            label6.Location = new Point(189, 221);
            label6.Name = "label6";
            label6.Size = new Size(17, 15);
            label6.TabIndex = 18;
            label6.Text = "%";
            label6.Click += label6_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 461);
            Controls.Add(label6);
            Controls.Add(label_aktivText);
            Controls.Add(label_aktivIcon);
            Controls.Add(label5);
            Controls.Add(num_groesse);
            Controls.Add(label4);
            Controls.Add(num_faktor);
            Controls.Add(label3);
            Controls.Add(textBox3);
            Controls.Add(btn_print);
            Controls.Add(btn_ausgabe);
            Controls.Add(label2);
            Controls.Add(textBox_ausgabe);
            Controls.Add(btn_ueberwachung);
            Controls.Add(label1);
            Controls.Add(textBox_ueberwachung);
            Controls.Add(btn_startStop);
            MaximumSize = new Size(500, 500);
            MinimumSize = new Size(500, 0);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Bilder für Button-Maschine generieren";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)num_faktor).EndInit();
            ((System.ComponentModel.ISupportInitialize)num_groesse).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_startStop;
        private ColorDialog colorDialog1;
        private TextBox textBox_ueberwachung;
        private Label label1;
        private Button btn_ueberwachung;
        private Button btn_ausgabe;
        private Label label2;
        private TextBox textBox_ausgabe;
        private Button btn_print;
        private TextBox textBox3;
        private Label label3;
        private NumericUpDown num_faktor;
        private NumericUpDown num_groesse;
        private Label label4;
        private Label label5;
        private Label label_aktivIcon;
        private Label label_aktivText;
        private Label label6;
    }
}