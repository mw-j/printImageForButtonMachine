using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public sealed class BildInfoTests : IDisposable
{
    private readonly string _ordner = Directory.CreateTempSubdirectory("bildinfo").FullName;
    private readonly string _foto;

    public BildInfoTests()
    {
        _foto = Path.Combine(_ordner, "IMG_1.JPG");
        File.WriteAllBytes(_foto, [1, 2, 3]);
    }

    public void Dispose() => Directory.Delete(_ordner, true);

    [Fact]
    public void PfadFuer_EndetNichtAufJpg()
    {
        Assert.Equal(@"C:\a\IMG_1.button.json", BildInfoSpeicher.PfadFuer(@"C:\a\IMG_1.JPG"));
    }

    [Fact]
    public void SpeichernUndLaden()
    {
        var info = new BildInfo([new Gesicht(0.1, 0.2, 0.3, 0.4)], new Ausschnitt(0.5, 0.6, 0.7));
        Assert.True(BildInfoSpeicher.Speichere(_foto, info));

        BildInfo? geladen = BildInfoSpeicher.Lade(_foto);

        Assert.NotNull(geladen);
        Assert.Equal(info.Gesichter, geladen.Gesichter);
        Assert.Equal(info.Manuell, geladen.Manuell);
    }

    [Fact]
    public void Lade_OhneDatei_Null() => Assert.Null(BildInfoSpeicher.Lade(_foto));

    [Fact]
    public void Lade_LeereDatei_Null()
    {
        File.WriteAllText(BildInfoSpeicher.PfadFuer(_foto), "");
        Assert.Null(BildInfoSpeicher.Lade(_foto));
    }

    [Fact]
    public void Lade_KaputtesJson_Null()
    {
        File.WriteAllText(BildInfoSpeicher.PfadFuer(_foto), "{\"Gesichter\": [");
        Assert.Null(BildInfoSpeicher.Lade(_foto));
    }

    [Fact]
    public void Speichere_FotoFehlt_FalseUndKeineDatei()
    {
        string fehlt = Path.Combine(_ordner, "weg.jpg");
        Assert.False(BildInfoSpeicher.Speichere(fehlt, new BildInfo([], null)));
        Assert.False(File.Exists(BildInfoSpeicher.PfadFuer(fehlt)));
    }

    [Fact]
    public void Waehle_Manuell_IgnoriertEinstellungen()
    {
        var info = new BildInfo([new Gesicht(0.3, 0.45, 0.1, 0.15)], new Ausschnitt(0.4, 0.5, 0.6));
        var a = AusschnittRechner.Waehle(info, 3000, 2000, 0.8, 5.2);
        var b = AusschnittRechner.Waehle(info, 3000, 2000, 0.5, 3.8);
        Assert.Equal(new Ausschnitt(0.4, 0.5, 0.6), a);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Waehle_Automatisch_FolgtFaktor()
    {
        var info = new BildInfo([], null);
        Assert.Equal(0.8, AusschnittRechner.Waehle(info, 3000, 2000, 0.8, 5.2).Seite, 1e-4);
        Assert.Equal(0.5, AusschnittRechner.Waehle(info, 3000, 2000, 0.5, 5.2).Seite, 1e-4);
    }
}
