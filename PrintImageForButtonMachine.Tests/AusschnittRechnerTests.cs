using System.Drawing;
using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public class AusschnittRechnerTests
{
    private const int Breite = 3000, Hoehe = 2000;
    private const double Faktor = 0.8, DurchmesserCm = 5.2;

    private static void Gleich(Ausschnitt erwartet, Ausschnitt ist, double toleranz = 1e-4)
    {
        Assert.Equal(erwartet.MitteX, ist.MitteX, toleranz);
        Assert.Equal(erwartet.MitteY, ist.MitteY, toleranz);
        Assert.Equal(erwartet.Seite, ist.Seite, toleranz);
    }

    [Fact]
    public void OhneGesichter_Mittig()
    {
        Gleich(new Ausschnitt(0.5, 0.5, 0.8), AusschnittRechner.Automatisch(Breite, Hoehe, [], Faktor, DurchmesserCm));
    }

    [Fact]
    public void OhneGesichter_Hochformat_Mittig()
    {
        Gleich(new Ausschnitt(0.5, 0.5, 0.8), AusschnittRechner.Automatisch(Hoehe, Breite, [], Faktor, DurchmesserCm));
    }

    [Fact]
    public void EinGesicht_MitteWandert_GroesseBleibt()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe, [new Gesicht(0.30, 0.45, 0.10, 0.15)], Faktor, DurchmesserCm);
        Gleich(new Ausschnitt(0.35, 0.50625, 0.8), ergebnis);
    }

    [Fact]
    public void EinGesicht_AmOberenRand_WirdBegrenzt()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe, [new Gesicht(0.45, 0.30, 0.10, 0.15)], Faktor, DurchmesserCm);
        Gleich(new Ausschnitt(0.5, 0.4, 0.8), ergebnis);
    }

    [Fact]
    public void ZweiGesichter_ZoomtHeraus()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe,
            [new Gesicht(0.25, 0.45, 0.10, 0.15), new Gesicht(0.65, 0.45, 0.10, 0.15)], Faktor, DurchmesserCm);
        Gleich(new Ausschnitt(0.5, 0.50625, 0.97890), ergebnis, 1e-3);
    }

    [Fact]
    public void DreiGesichter_HoechstensKuerzereKante()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe,
            [new Gesicht(0.20, 0.45, 0.10, 0.15), new Gesicht(0.45, 0.45, 0.10, 0.15), new Gesicht(0.70, 0.45, 0.10, 0.15)],
            Faktor, DurchmesserCm);
        Gleich(new Ausschnitt(0.5, 0.5, 1.0), ergebnis);
    }

    [Fact]
    public void KleinesGesicht_ZoomtNieHinein()
    {
        var ergebnis = AusschnittRechner.Automatisch(Breite, Hoehe, [new Gesicht(0.49, 0.45, 0.02, 0.03)], Faktor, DurchmesserCm);
        Assert.Equal(0.8, ergebnis.Seite, 1e-4);
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
