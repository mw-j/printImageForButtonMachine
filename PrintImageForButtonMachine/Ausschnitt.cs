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

        // Abgeleitet aus Handkorrekturen vom ersten Einsatz (Button 5,8 cm), bezogen auf den sichtbaren Kreis:
        // Das größte Gesicht ist so hoch wie dieser Anteil des sichtbaren Durchmessers ...
        private const double GesichtsAnteil = 0.61;
        // ... und der Rahmen um alle Gesichter reicht mit seiner halben Diagonale bis zu diesem Anteil des sichtbaren Radius.
        private const double FuellGrad = 0.93;

        /// <summary>
        /// Mitte auf den Gesichtern; die Größe richtet sich nach dem größten Gesicht oder, bei Gruppen, nach dem Abstand
        /// der Gesichter. Ohne Gesicht: mittig über die kürzere Bildkante.
        /// </summary>
        public static Ausschnitt Automatisch(int breite, int hoehe, IReadOnlyList<Gesicht> gesichter, double durchmesserCm)
        {
            double kurz = Math.Min(breite, hoehe);
            if (gesichter.Count == 0) return new Ausschnitt(0.5, 0.5, 1);

            double l = gesichter.Min(g => g.X) * breite;
            double r = gesichter.Max(g => g.X + g.Breite) * breite;
            double o = gesichter.Min(g => g.Y) * hoehe;
            double u = gesichter.Max(g => g.Y + g.Hoehe) * hoehe;
            double innenAnteil = (durchmesserCm - 2 * RandCm) / durchmesserCm;
            double nachGesicht = gesichter.Max(g => g.Hoehe) * hoehe / (GesichtsAnteil * innenAnteil);
            double nachAbstand = 2 * Math.Sqrt(Math.Pow((r - l) / 2, 2) + Math.Pow((u - o) / 2, 2)) / (FuellGrad * innenAnteil);
            double seite = Math.Max(nachGesicht, nachAbstand);
            return Begrenze(new Ausschnitt((l + r) / 2 / breite, (o + u) / 2 / hoehe, seite / kurz), breite, hoehe);
        }

        /// <summary>
        /// Handkorrektur hat Vorrang; automatische Ausschnitte folgen dem eingestellten Durchmesser.
        /// </summary>
        public static Ausschnitt Waehle(BildInfo? info, int breite, int hoehe, double durchmesserCm)
        {
            if (info?.Manuell != null) return Begrenze(info.Manuell, breite, hoehe);
            return Automatisch(breite, hoehe, info?.Gesichter ?? [], durchmesserCm);
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
