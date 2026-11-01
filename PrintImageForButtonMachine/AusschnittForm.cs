using System.Drawing.Drawing2D;

namespace PrintImageForButtonMachine
{
    /// <summary>
    /// Fester Kreis, darunter das Foto: Ziehen verschiebt, das Mausrad zoomt.
    /// </summary>
    internal class AusschnittForm : Form
    {
        private const int MaxAnzeigeKante = 2500; // Arbeitskopie, damit Ziehen und Zoomen auch auf älteren Rechnern flüssig bleiben
        private const double KreisAnteil = 0.8; // Anteil der kürzeren Seite der Zeichenfläche
        private static readonly Color Hintergrund = Color.FromArgb(0x2B, 0x2B, 0x2B);
        private static readonly Color Gelb = Color.FromArgb(0xFF, 0xD4, 0x00);

        private readonly Bitmap _anzeige;
        private readonly Ausschnitt _automatisch;
        private readonly double _durchmesserCm;
        private readonly Panel _flaeche = new ZeichenFlaeche { Dock = DockStyle.Fill, BackColor = Hintergrund, Cursor = Cursors.SizeAll };
        private Ausschnitt _aktuell;
        private bool _istAutomatisch;
        private Point? _zugStart;
        private Ausschnitt? _zugAusschnitt;

        /// <summary>
        /// Nach OK: null, wenn der automatische Ausschnitt gelten soll, sonst die Handkorrektur.
        /// </summary>
        public Ausschnitt? Ergebnis { get; private set; }

        public AusschnittForm(Image bild, string dateiname, Ausschnitt start, Ausschnitt automatisch, double durchmesserCm, int anzahlGesichter)
        {
            _anzeige = ErzeugeArbeitskopie(bild);
            _automatisch = automatisch;
            _durchmesserCm = durchmesserCm;
            _aktuell = start;
            _istAutomatisch = start == automatisch;

            Text = $"Bildausschnitt bearbeiten – {dateiname}";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(680, 560);
            MinimumSize = new Size(420, 360);
            ShowInTaskbar = false;
            MinimizeBox = false;

            var hinweis = new Label
            {
                Text = $"Ziehen = verschieben · Mausrad = zoomen · {anzahlGesichter} Gesichter erkannt",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            };
            var automatischKnopf = new Button { Text = "Automatisch", AutoSize = true };
            var abbrechen = new Button { Text = "Abbrechen", AutoSize = true, DialogResult = DialogResult.Cancel };
            var ok = new Button { Text = "OK", AutoSize = true, DialogResult = DialogResult.OK };
            automatischKnopf.Click += (s, e) => { _aktuell = _automatisch; _istAutomatisch = true; _flaeche.Invalidate(); };
            ok.Click += (s, e) => Ergebnis = _istAutomatisch ? null : _aktuell;

            var leiste = new TableLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 4, Padding = new Padding(8) };
            leiste.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            leiste.Controls.Add(hinweis);
            leiste.Controls.Add(automatischKnopf);
            leiste.Controls.Add(abbrechen);
            leiste.Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = abbrechen;

            Controls.Add(_flaeche);
            Controls.Add(leiste);

            _flaeche.Paint += Zeichne;
            _flaeche.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _zugStart = e.Location; _zugAusschnitt = _aktuell; } };
            _flaeche.MouseUp += (s, e) => _zugStart = null;
            _flaeche.MouseMove += Ziehe;
            _flaeche.MouseWheel += Zoome;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _anzeige.Dispose();
            base.OnFormClosed(e);
        }

        private static Bitmap ErzeugeArbeitskopie(Image bild)
        {
            double skala = Math.Min(1, (double)MaxAnzeigeKante / Math.Max(bild.Width, bild.Height));
            var kopie = new Bitmap(Math.Max(1, (int)(bild.Width * skala)), Math.Max(1, (int)(bild.Height * skala)));
            using Graphics g = Graphics.FromImage(kopie);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bild, 0, 0, kopie.Width, kopie.Height);
            return kopie;
        }

        private RectangleF Kreis()
        {
            float d = (float)(Math.Min(_flaeche.ClientSize.Width, _flaeche.ClientSize.Height) * KreisAnteil);
            return new RectangleF((_flaeche.ClientSize.Width - d) / 2, (_flaeche.ClientSize.Height - d) / 2, d, d);
        }

        /// <summary>
        /// Bildschirmpixel je Pixel der Arbeitskopie.
        /// </summary>
        private double Skala(Ausschnitt a) => Kreis().Width / (a.Seite * Math.Min(_anzeige.Width, _anzeige.Height));

        private RectangleF BildRechteck(Ausschnitt a)
        {
            RectangleF kreis = Kreis();
            double s = Skala(a);
            float x = (float)(kreis.X + kreis.Width / 2 - a.MitteX * _anzeige.Width * s);
            float y = (float)(kreis.Y + kreis.Height / 2 - a.MitteY * _anzeige.Height * s);
            return new RectangleF(x, y, (float)(_anzeige.Width * s), (float)(_anzeige.Height * s));
        }

        private void Setze(Ausschnitt a)
        {
            _aktuell = AusschnittRechner.Begrenze(a, _anzeige.Width, _anzeige.Height);
            _istAutomatisch = false;
            _flaeche.Invalidate();
        }

        private void Ziehe(object? sender, MouseEventArgs e)
        {
            if (_zugStart is not Point start || _zugAusschnitt is not Ausschnitt basis) return;
            double s = Skala(basis);
            double dx = (e.X - start.X) / (s * _anzeige.Width);
            double dy = (e.Y - start.Y) / (s * _anzeige.Height);
            Setze(basis with { MitteX = basis.MitteX - dx, MitteY = basis.MitteY - dy });
        }

        private void Zoome(object? sender, MouseEventArgs e)
        {
            RectangleF bild = BildRechteck(_aktuell);
            double punktX = (e.X - bild.X) / bild.Width;
            double punktY = (e.Y - bild.Y) / bild.Height;
            Setze(AusschnittRechner.Zoome(_aktuell, Math.Pow(1.1, -e.Delta / 120.0), punktX, punktY));
        }

        private void Zeichne(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawImage(_anzeige, BildRechteck(_aktuell));

            RectangleF kreis = Kreis();
            using (var pfad = new GraphicsPath())
            using (var aussen = new Region(_flaeche.ClientRectangle))
            using (var schleier = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                pfad.AddEllipse(kreis);
                aussen.Exclude(pfad);
                g.FillRegion(schleier, aussen);
            }

            float innenD = (float)(kreis.Width * (_durchmesserCm - 2 * AusschnittRechner.RandCm) / _durchmesserCm);
            var innen = new RectangleF(kreis.X + (kreis.Width - innenD) / 2, kreis.Y + (kreis.Height - innenD) / 2, innenD, innenD);
            using (var weiss = new Pen(Color.White, 2))
            using (var gestrichelt = new Pen(Gelb, 2) { DashStyle = DashStyle.Dash })
            {
                g.DrawEllipse(weiss, kreis);
                g.DrawEllipse(gestrichelt, innen);
            }

            int mm = (int)Math.Round(_durchmesserCm * 10);
            Beschrifte(g, $"Druckrand {mm} mm", Color.White, new PointF(kreis.Right - 40, kreis.Top + 4));
            Beschrifte(g, $"sichtbar {mm - 8} mm", Gelb, new PointF(innen.Right - 30, innen.Bottom - 20));
        }

        private void Beschrifte(Graphics g, string text, Color farbe, PointF ort)
        {
            SizeF groesse = g.MeasureString(text, Font);
            using var hintergrund = new SolidBrush(Color.FromArgb(140, 0, 0, 0));
            using var schrift = new SolidBrush(farbe);
            g.FillRectangle(hintergrund, ort.X - 3, ort.Y - 1, groesse.Width + 6, groesse.Height + 2);
            g.DrawString(text, Font, schrift, ort);
        }

        private class ZeichenFlaeche : Panel
        {
            public ZeichenFlaeche()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }
    }
}
