using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public sealed class ArchivTests : IDisposable
{
    private readonly string _quelle = Directory.CreateTempSubdirectory("quelle").FullName;
    private readonly string _ausgabe = Directory.CreateTempSubdirectory("ausgabe").FullName;

    public void Dispose()
    {
        Directory.Delete(_quelle, true);
        Directory.Delete(_ausgabe, true);
    }

    private FileInfo LegeFotoMitBegleitdateiAn(string name)
    {
        string pfad = Path.Combine(_quelle, name);
        File.WriteAllBytes(pfad, [1, 2, 3]);
        BildInfoSpeicher.Speichere(pfad, new BildInfo([], null));
        return new FileInfo(pfad);
    }

    [Fact]
    public void Archiviere_VerschiebtBegleitdateienMit()
    {
        FileInfo[] fotos = [LegeFotoMitBegleitdateiAn("a.jpg"), LegeFotoMitBegleitdateiAn("b.jpg")];
        string ziel = Path.Combine(_ausgabe, "druck");

        ImageProcessor.Archiviere(fotos, ziel);

        Assert.Empty(Directory.GetFiles(_quelle));
        Assert.True(File.Exists(Path.Combine(ziel, "a.jpg")));
        Assert.True(File.Exists(Path.Combine(ziel, "a.button.json")));
        Assert.True(File.Exists(Path.Combine(ziel, "b.jpg")));
        Assert.True(File.Exists(Path.Combine(ziel, "b.button.json")));
    }

    [Fact]
    public void Archiviere_GesperrteDatei_AllesZurueck()
    {
        FileInfo[] fotos = [LegeFotoMitBegleitdateiAn("a.jpg"), LegeFotoMitBegleitdateiAn("b.jpg")];
        string ziel = Path.Combine(_ausgabe, "druck");

        using (new FileStream(fotos[1].FullName, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsAny<IOException>(() => ImageProcessor.Archiviere(fotos, ziel));
        }

        Assert.True(File.Exists(Path.Combine(_quelle, "a.jpg")));
        Assert.True(File.Exists(Path.Combine(_quelle, "a.button.json")));
        Assert.True(File.Exists(Path.Combine(_quelle, "b.jpg")));
        Assert.True(File.Exists(Path.Combine(_quelle, "b.button.json")));
        Assert.False(Directory.Exists(ziel));
    }
}
