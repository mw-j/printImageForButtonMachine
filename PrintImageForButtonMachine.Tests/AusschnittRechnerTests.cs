using System.Drawing;
using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public class AusschnittRechnerTests
{
    private const int Breite = 3000, Hoehe = 2000;
    private const double DurchmesserCm = 5.8;

    private static void Gleich(Ausschnitt erwartet, Ausschnitt ist, double toleranz = 1e-4)
    {
        Assert.Equal(erwartet.MitteX, ist.MitteX, toleranz);
        Assert.Equal(erwartet.MitteY, ist.MitteY, toleranz);
        Assert.Equal(erwartet.Seite, ist.Seite, toleranz);
    }

    [Fact]
    public void OhneGesichter_MittigVolleHoehe()
    {
        Gleich(new Ausschnitt(0.5, 0.5, 1.0), AusschnittRechner.Automatisch(Breite, Hoehe, [], DurchmesserCm));
    }

    [Fact]
    public void OhneGesichter_Hochformat_MittigVolleBreite()
    {
        Gleich(new Ausschnitt(0.5, 0.5, 1.0), AusschnittRechner.Automatisch(Hoehe, Breite, [], DurchmesserCm));
    }

    [Fact]
    public void EinGesicht_GesichtshoeheBestimmtGroesse()
    {
        // Gesicht 240 px hoch → 61 % des sichtbaren Kreises (Anteil 5,0 / 5,8) → Seite 456,4 px
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe, [new Gesicht(0.45, 0.40, 0.06, 0.12)], DurchmesserCm);
        Gleich(new Ausschnitt(0.48, 0.46, 0.22820), ergebnis);
    }

    [Fact]
    public void ZweiGesichter_AbstandBestimmtGroesse()
    {
        // Rahmen 900..1980 × 800..1080, halbe Diagonale 557,9 px = 93 % des sichtbaren Radius → Seite 1391,6 px
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe,
            [new Gesicht(0.30, 0.40, 0.06, 0.12), new Gesicht(0.60, 0.42, 0.06, 0.12)], DurchmesserCm);
        Gleich(new Ausschnitt(0.48, 0.47, 0.69582), ergebnis, 2e-4);
    }

    [Fact]
    public void WeitVerteilteGesichter_HoechstensKuerzereKante()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe,
            [new Gesicht(0.10, 0.40, 0.06, 0.12), new Gesicht(0.45, 0.40, 0.06, 0.12), new Gesicht(0.80, 0.40, 0.06, 0.12)],
            DurchmesserCm);
        Assert.Equal(1.0, ergebnis.Seite, 1e-4);
        Assert.Equal(0.5, ergebnis.MitteY, 1e-4);
    }

    [Fact]
    public void GesichtAmRand_WirdInsBildGeschoben()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe, [new Gesicht(0.0, 0.40, 0.06, 0.12)], DurchmesserCm);
        Assert.Equal(456.39 / 2 / Breite, ergebnis.MitteX, 1e-4);
    }

    [Fact]
    public void KleinererButton_GroessererAusschnitt()
    {
        Gesicht[] gesicht = [new Gesicht(0.45, 0.40, 0.06, 0.12)];
        double gross = AusschnittRechner.Automatisch(Breite, Hoehe, gesicht, 5.8).Seite;
        double klein = AusschnittRechner.Automatisch(Breite, Hoehe, gesicht, 3.2).Seite;
        Assert.True(klein > gross);
    }

    [Fact]
    public void Begrenze_SchiebtInsBild()
    {
        Gleich(new Ausschnitt(0.73333, 0.5, 0.8), AusschnittRechner.Begrenze(new Ausschnitt(0.95, 0.5, 0.8), Breite, Hoehe));
    }

    [Fact]
    public void Begrenze_SeiteZwischen10Und100Prozent()
    {
        Assert.Equal(1.0, AusschnittRechner.Begrenze(new Ausschnitt(0.5, 0.5, 1.5), Breite, Hoehe).Seite, 1e-4);
        Assert.Equal(0.1, AusschnittRechner.Begrenze(new Ausschnitt(0.5, 0.5, 0.01), Breite, Hoehe).Seite, 1e-4);
    }

    [Fact]
    public void Zoome_UmMitte()
    {
        Gleich(new Ausschnitt(0.5, 0.5, 0.4), AusschnittRechner.Zoome(new Ausschnitt(0.5, 0.5, 0.8), 0.5, 0.5, 0.5));
    }

    [Fact]
    public void Zoome_UmPunkt_PunktBleibtStehen()
    {
        Gleich(new Ausschnitt(0.6, 0.5, 0.4), AusschnittRechner.Zoome(new Ausschnitt(0.5, 0.5, 0.8), 0.5, 0.7, 0.5));
    }

    [Fact]
    public void InPixel_Rechteck()
    {
        Assert.Equal(new RectangleF(700, 200, 1600, 1600), AusschnittRechner.InPixel(new Ausschnitt(0.5, 0.5, 0.8), Breite, Hoehe));
    }
}
