namespace PrintImageForButtonMachine
{
    /// <summary>
    /// Erkanntes Gesicht, relativ zu Bildbreite und -höhe (0..1).
    /// </summary>
    internal record Gesicht(double X, double Y, double Breite, double Hoehe);

    /// <summary>
    /// Quadratischer Bildausschnitt: Mitte relativ zu Breite/Höhe, Seite relativ zur kürzeren Bildkante.
    /// </summary>
    internal record Ausschnitt(double MitteX, double MitteY, double Seite);

    internal static class AusschnittRechner
    {
        /// <summary>
        /// Breite des Rands, der beim Pressen umgeschlagen wird und auf dem Button nicht sichtbar ist.
        /// </summary>
        public const double RandCm = 0.4;
        private const double MinSeite = 0.1;

        // Zuschlag um jedes Gesicht, damit Haare und Kinn mit in den sichtbaren Bereich kommen
        private const double ZuschlagSeitlich = 0.15, ZuschlagOben = 0.40, ZuschlagUnten = 0.15;

        /// <summary>
        /// Größe aus der Prozent-Einstellung, Mitte auf den Gesichtern. Herausgezoomt wird nur, wenn die Gesichter
        /// sonst nicht in den sichtbaren Bereich passen, hineingezoomt nie.
        /// </summary>
        /// <param name="faktor">Anteil der kürzeren Bildkante [0,1]</param>
        public static Ausschnitt Automatisch(int breite, int hoehe, IReadOnlyList<Gesicht> gesichter, double faktor, double durchmesserCm)
        {
            double kurz = Math.Min(breite, hoehe);
            double seite = kurz * faktor;
            double cx = breite / 2.0, cy = hoehe / 2.0;
            if (gesichter.Count > 0)
            {
                double l = gesichter.Min(g => (g.X - g.Breite * ZuschlagSeitlich) * breite);
                double r = gesichter.Max(g => (g.X + g.Breite * (1 + ZuschlagSeitlich)) * breite);
                double o = gesichter.Min(g => (g.Y - g.Hoehe * ZuschlagOben) * hoehe);
                double u = gesichter.Max(g => (g.Y + g.Hoehe * (1 + ZuschlagUnten)) * hoehe);
                cx = (l + r) / 2;
                cy = (o + u) / 2;
                double innenAnteil = (durchmesserCm - 2 * RandCm) / durchmesserCm;
                double benoetigt = 2 * Math.Sqrt(Math.Pow((r - l) / 2, 2) + Math.Pow((u - o) / 2, 2)) / innenAnteil;
                if (benoetigt > seite) seite = Math.Min(benoetigt, kurz);
            }
            return Begrenze(new Ausschnitt(cx / breite, cy / hoehe, seite / kurz), breite, hoehe);
        }

        /// <summary>
        /// Handkorrektur hat Vorrang; automatische Ausschnitte folgen den aktuellen Einstellungen.
        /// </summary>
        public static Ausschnitt Waehle(BildInfo? info, int breite, int hoehe, double faktor, double durchmesserCm)
        {
            if (info?.Manuell != null) return Begrenze(info.Manuell, breite, hoehe);
            return Automatisch(breite, hoehe, info?.Gesichter ?? [], faktor, durchmesserCm);
        }

        /// <summary>
        /// Hält den Ausschnitt vollständig im Bild, damit der Kreis nie einen leeren Rand bekommt.
        /// </summary>
        public static Ausschnitt Begrenze(Ausschnitt a, int breite, int hoehe)
        {
            double seite = Math.Clamp(a.Seite, MinSeite, 1);
            double halb = seite * Math.Min(breite, hoehe) / 2;
            double cx = Math.Clamp(a.MitteX * breite, halb, breite - halb);
            double cy = Math.Clamp(a.MitteY * hoehe, halb, hoehe - halb);
            return new Ausschnitt(cx / breite, cy / hoehe, seite);
        }

        /// <summary>
        /// Ändert die Größe um <paramref name="faktor"/>; der Bildpunkt (relativ) bleibt an seiner Position im Kreis.
        /// </summary>
        public static Ausschnitt Zoome(Ausschnitt a, double faktor, double punktX, double punktY)
        {
            return new Ausschnitt(punktX + (a.MitteX - punktX) * faktor, punktY + (a.MitteY - punktY) * faktor, a.Seite * faktor);
        }

        public static RectangleF InPixel(Ausschnitt a, int breite, int hoehe)
        {
            float seite = (float)(a.Seite * Math.Min(breite, hoehe));
            return new RectangleF((float)(a.MitteX * breite - seite / 2), (float)(a.MitteY * hoehe - seite / 2), seite, seite);
        }
    }
}
