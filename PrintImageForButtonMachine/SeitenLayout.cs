namespace PrintImageForButtonMachine
{
    /// <summary>
    /// Anordnung der Kreise auf einer A4-Seite, alle Angaben in cm.
    /// </summary>
    internal class SeitenLayout
    {
        public const double SeitenBreiteCm = 21; // A4
        public const double SeitenHoeheCm = 29.7;
        public const double AbstandCm = 0.5;
        private readonly double _groesseCm;

        public SeitenLayout(double groesseCm)
        {
            _groesseCm = groesseCm;
        }

        public int Spalten => ErmittleAnzahl(SeitenBreiteCm);
        public int Zeilen => ErmittleAnzahl(SeitenHoeheCm);
        public int BilderProSeite => Spalten * Zeilen;

        public RectangleF KreisRechteckCm(int index)
        {
            int spalten = Math.Max(Spalten, 1);
            double x = AbstandCm + (index % spalten) * (_groesseCm + AbstandCm);
            double y = AbstandCm + (index / spalten) * (_groesseCm + AbstandCm);
            return new RectangleF((float)x, (float)y, (float)_groesseCm, (float)_groesseCm);
        }

        /// <returns>Index des Kreises, in dessen Kreisfläche der Punkt liegt, sonst null.</returns>
        public int? IndexAn(double xCm, double yCm, int anzahlBilder)
        {
            for (int i = 0; i < anzahlBilder; i++)
            {
                RectangleF r = KreisRechteckCm(i);
                double dx = xCm - (r.X + r.Width / 2.0), dy = yCm - (r.Y + r.Height / 2.0);
                if (dx * dx + dy * dy <= Math.Pow(_groesseCm / 2, 2)) return i;
            }
            return null;
        }

        private int ErmittleAnzahl(double verfuegbareLaengeCm)
        {
            return (int)((verfuegbareLaengeCm - AbstandCm) / (AbstandCm + _groesseCm));
        }
    }
}
