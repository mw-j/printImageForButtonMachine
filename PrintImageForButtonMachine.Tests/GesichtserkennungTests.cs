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
}
