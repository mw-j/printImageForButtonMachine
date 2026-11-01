using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public sealed class GesichtserkennungTests : IDisposable
{
    private readonly string _ordner = Directory.CreateTempSubdirectory("erkennung").FullName;

    public void Dispose() => Directory.Delete(_ordner, true);

    private FileInfo LegeFotoAn(string name, byte[] inhalt)
    {
        string pfad = Path.Combine(_ordner, name);
        File.WriteAllBytes(pfad, inhalt);
        return new FileInfo(pfad);
    }

    [Fact]
    public async Task ErgaenzeFehlende_UnvollstaendigesFoto_KeineBegleitdatei()
    {
        FileInfo foto = LegeFotoAn("halb.jpg", TestBilder.Jpeg(400, 300).Take(100).ToArray());

        await Gesichtserkennung.ErgaenzeFehlendeAsync([foto]);

        Assert.False(File.Exists(BildInfoSpeicher.PfadFuer(foto.FullName)));
    }

    [Fact]
    public async Task ErgaenzeFehlende_FotoOhneGesicht_BegleitdateiMitLeererListe()
    {
        FileInfo foto = LegeFotoAn("leer.jpg", TestBilder.Jpeg(400, 300));

        await Gesichtserkennung.ErgaenzeFehlendeAsync([foto]);

        BildInfo? info = BildInfoSpeicher.Lade(foto.FullName);
        Assert.NotNull(info);
        Assert.Empty(info.Gesichter);
        Assert.Null(info.Manuell);
    }

    [Fact]
    public async Task ErgaenzeFehlende_VorhandeneBegleitdatei_BleibtUnveraendert()
    {
        FileInfo foto = LegeFotoAn("bearbeitet.jpg", TestBilder.Jpeg(400, 300));
        var vorher = new BildInfo([new Gesicht(0.1, 0.1, 0.2, 0.2)], new Ausschnitt(0.4, 0.5, 0.6));
        BildInfoSpeicher.Speichere(foto.FullName, vorher);

        await Gesichtserkennung.ErgaenzeFehlendeAsync([foto]);

        BildInfo? nachher = BildInfoSpeicher.Lade(foto.FullName);
        Assert.NotNull(nachher);
        Assert.Equal(vorher.Gesichter, nachher.Gesichter);
        Assert.Equal(vorher.Manuell, nachher.Manuell);
    }

    [Fact]
    public async Task ErgaenzeFehlende_FotoNachSchreibenFertig_WirdNeuErkannt()
    {
        byte[] ganz = TestBilder.Jpeg(400, 300);
        FileInfo foto = LegeFotoAn("im_schreiben.jpg", ganz.Take(ganz.Length / 2).ToArray());
        await Gesichtserkennung.ErgaenzeFehlendeAsync([foto]);

        File.WriteAllBytes(foto.FullName, ganz);

        // Die Begleitdatei gehört zum halben Foto und darf für das fertige nicht mehr gelten
        Assert.Null(BildInfoSpeicher.Lade(foto.FullName));
        await Gesichtserkennung.ErgaenzeFehlendeAsync([new FileInfo(foto.FullName)]);
        Assert.NotNull(BildInfoSpeicher.Lade(foto.FullName));
    }

    [Fact]
    public async Task ErgaenzeFehlende_BegleitdateiNichtSchreibbar_KeinFehler()
    {
        FileInfo foto = LegeFotoAn("gesperrt.jpg", TestBilder.Jpeg(400, 300));
        Directory.CreateDirectory(BildInfoSpeicher.PfadFuer(foto.FullName));

        await Gesichtserkennung.ErgaenzeFehlendeAsync([foto]);
    }

    [Fact]
    public async Task ErgaenzeFehlende_BegleitdateiKurzNichtLesbar_HandkorrekturBleibt()
    {
        FileInfo foto = LegeFotoAn("korrigiert.jpg", TestBilder.Jpeg(400, 300));
        BildInfoSpeicher.Speichere(foto.FullName, new BildInfo([], new Ausschnitt(0.4, 0.5, 0.6)));

        // Lesen verboten, Ersetzen erlaubt: z. B. ein Virenscanner, der die Datei gerade prüft
        using (new FileStream(BildInfoSpeicher.PfadFuer(foto.FullName), FileMode.Open, FileAccess.Read, FileShare.Write | FileShare.Delete))
        {
            await Gesichtserkennung.ErgaenzeFehlendeAsync([foto]);
        }

        Assert.Equal(new Ausschnitt(0.4, 0.5, 0.6), BildInfoSpeicher.Lade(foto.FullName)?.Manuell);
    }
}
