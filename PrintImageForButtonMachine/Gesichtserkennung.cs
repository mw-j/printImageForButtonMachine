using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;

namespace PrintImageForButtonMachine
{
    /// <summary>
    /// Findet Gesichter mit der in Windows eingebauten Erkennung (offline, ohne Zusatzbibliothek).
    /// </summary>
    internal static class Gesichtserkennung
    {
        private const int ErkennungsKante = 1500; // genügt für Gesichter und hält die Erkennung auch auf älteren Rechnern schnell
        private static readonly Lazy<Task<FaceDetector>> Detektor = new(() => FaceDetector.CreateAsync().AsTask());

        /// <returns>Leere Liste, wenn Windows keine Gesichtserkennung bietet (z. B. N-Editionen) oder sie fehlschlägt.</returns>
        public static async Task<List<Gesicht>> ErkenneAsync(Image bild)
        {
            try
            {
                if (!FaceDetector.IsSupported) return [];
                (byte[] grau, int breite, int hoehe) = await Task.Run(() => ErzeugeGraustufen(bild));
                using var bitmap = new SoftwareBitmap(BitmapPixelFormat.Gray8, breite, hoehe);
                bitmap.CopyFromBuffer(grau.AsBuffer());
                FaceDetector detektor = await Detektor.Value;
                IList<DetectedFace> gesichter = await detektor.DetectFacesAsync(bitmap);
                return gesichter
                    .Select(g => new Gesicht((double)g.FaceBox.X / breite, (double)g.FaceBox.Y / hoehe, (double)g.FaceBox.Width / breite, (double)g.FaceBox.Height / hoehe))
                    .ToList();
            }
            catch (Exception)
            {
                // Ohne Erkennung bleibt es beim mittigen Ausschnitt; das Programm soll deswegen nicht stehen bleiben
                return [];
            }
        }

        /// <summary>
        /// Erkennt Gesichter für alle Fotos, zu denen es noch keine gültige Begleitdatei gibt.
        /// Fotos, die sich (noch) nicht laden lassen, werden im nächsten Durchlauf erneut versucht.
        /// </summary>
        public static async Task ErgaenzeFehlendeAsync(IEnumerable<FileInfo> fotos)
        {
            foreach (FileInfo foto in fotos)
            {
                if (BildInfoSpeicher.Lade(foto.FullName) != null) continue;
                using Image? bild = await Task.Run(() => BildLader.LadeBild(foto.FullName));
                if (bild == null) continue;
                List<Gesicht> gesichter = await ErkenneAsync(bild);
                BildInfoSpeicher.Speichere(foto.FullName, new BildInfo(gesichter, null));
            }
        }

        private static (byte[] Grau, int Breite, int Hoehe) ErzeugeGraustufen(Image bild)
        {
            double skala = Math.Min(1, (double)ErkennungsKante / Math.Max(bild.Width, bild.Height));
            int breite = Math.Max(1, (int)(bild.Width * skala)), hoehe = Math.Max(1, (int)(bild.Height * skala));
            using var klein = new Bitmap(breite, hoehe, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(klein))
            {
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.DrawImage(bild, 0, 0, breite, hoehe);
            }
            BitmapData daten = klein.LockBits(new Rectangle(0, 0, breite, hoehe), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] bgra = new byte[daten.Stride * hoehe];
            Marshal.Copy(daten.Scan0, bgra, 0, bgra.Length);
            klein.UnlockBits(daten);

            byte[] grau = new byte[breite * hoehe];
            for (int y = 0; y < hoehe; y++)
            {
                for (int x = 0; x < breite; x++)
                {
                    int i = y * daten.Stride + x * 4;
                    grau[y * breite + x] = (byte)((bgra[i + 2] * 77 + bgra[i + 1] * 150 + bgra[i] * 29) >> 8);
                }
            }
            return (grau, breite, hoehe);
        }
    }
}
