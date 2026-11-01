using System.Drawing;
using System.Drawing.Imaging;
using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public sealed class BildLaderTests : IDisposable
{
    private readonly string _ordner = Directory.CreateTempSubdirectory("bildlader").FullName;

    public void Dispose() => Directory.Delete(_ordner, true);

    [Theory]
    [InlineData(1, RotateFlipType.RotateNoneFlipNone)]
    [InlineData(2, RotateFlipType.RotateNoneFlipX)]
    [InlineData(3, RotateFlipType.Rotate180FlipNone)]
    [InlineData(4, RotateFlipType.Rotate180FlipX)]
    [InlineData(5, RotateFlipType.Rotate90FlipX)]
    [InlineData(6, RotateFlipType.Rotate90FlipNone)]
    [InlineData(7, RotateFlipType.Rotate270FlipX)]
    [InlineData(8, RotateFlipType.Rotate270FlipNone)]
    [InlineData(0, RotateFlipType.RotateNoneFlipNone)]
    [InlineData(99, RotateFlipType.RotateNoneFlipNone)]
    public void DrehungFuerExif(int orientierung, RotateFlipType erwartet)
    {
        Assert.Equal(erwartet, BildLader.DrehungFuerExif(orientierung));
    }

    [Fact]
    public void LadeBild_AbgeschnitteneDatei_Null()
    {
        string pfad = Path.Combine(_ordner, "halb.jpg");
        File.WriteAllBytes(pfad, TestBilder.Jpeg(30, 20).Take(100).ToArray());
        Assert.Null(BildLader.LadeBild(pfad));
    }

    [Fact]
    public void LadeBild_OhneExif_Unveraendert()
    {
        string pfad = Path.Combine(_ordner, "ganz.jpg");
        File.WriteAllBytes(pfad, TestBilder.Jpeg(30, 20));
        using Image? bild = BildLader.LadeBild(pfad);
        Assert.NotNull(bild);
        Assert.Equal(new Size(30, 20), bild.Size);
    }
}

internal static class TestBilder
{
    public static byte[] Jpeg(int breite, int hoehe)
    {
        using var bmp = new Bitmap(breite, hoehe);
        using (Graphics g = Graphics.FromImage(bmp)) g.Clear(Color.CornflowerBlue);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Jpeg);
        return ms.ToArray();
    }
}
