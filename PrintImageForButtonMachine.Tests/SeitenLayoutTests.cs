using System.Drawing;
using PrintImageForButtonMachine;

namespace PrintImageForButtonMachine.Tests;

public class SeitenLayoutTests
{
    private readonly SeitenLayout _layout = new(5.2);

    [Fact]
    public void Raster_52mm()
    {
        Assert.Equal(3, _layout.Spalten);
        Assert.Equal(5, _layout.Zeilen);
        Assert.Equal(15, _layout.BilderProSeite);
    }

    [Fact]
    public void Treffer_Kreismitte0() => Assert.Equal(0, _layout.IndexAn(3.1, 3.1, 5));

    [Fact]
    public void Treffer_Kreismitte4() => Assert.Equal(4, _layout.IndexAn(8.8, 8.8, 5));

    [Fact]
    public void KeinTreffer_Zwischenraum() => Assert.Null(_layout.IndexAn(5.95, 3.1, 5));

    [Fact]
    public void KeinTreffer_EckeAusserhalbKreis() => Assert.Null(_layout.IndexAn(0.6, 0.6, 5));

    [Fact]
    public void KeinTreffer_OhneBild() => Assert.Null(_layout.IndexAn(8.8, 8.8, 4));

    [Fact]
    public void KreisRechteck_Index4()
    {
        RectangleF r = _layout.KreisRechteckCm(4);
        Assert.Equal(6.2f, r.X, 1e-4f);
        Assert.Equal(6.2f, r.Y, 1e-4f);
        Assert.Equal(5.2f, r.Width, 1e-4f);
        Assert.Equal(5.2f, r.Height, 1e-4f);
    }
}
