using System.Runtime.InteropServices;

namespace PrintImageForButtonMachine
{
    internal static class BildLader
    {
        private const int ExifOrientierung = 0x0112;

        /// <summary>
        /// Lädt das Bild vollständig in den Speicher, damit die Datei nicht für die Dauer der Verarbeitung gesperrt bleibt,
        /// und dreht es so, wie es laut EXIF aufgenommen wurde (z. B. hochkant gehaltene Kamera).
        /// Liefert null, wenn die Datei (noch) nicht gelesen werden kann.
        /// </summary>
        public static Image? LadeBild(string pfad)
        {
            try
            {
                // Der MemoryStream muss so lange leben wie das Bild; er hält keine nativen Ressourcen
                Image bild = Image.FromStream(new MemoryStream(File.ReadAllBytes(pfad)));
                if (bild.PropertyIdList.Contains(ExifOrientierung))
                {
                    byte[]? wert = bild.GetPropertyItem(ExifOrientierung)?.Value;
                    if (wert is { Length: >= 2 })
                    {
                        bild.RotateFlip(DrehungFuerExif(BitConverter.ToUInt16(wert, 0)));
                    }
                    // Sonst würde ein Programm, das das Bild später auswertet, es ein zweites Mal drehen
                    bild.RemovePropertyItem(ExifOrientierung);
                }
                return bild;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is OutOfMemoryException || ex is ExternalException)
            {
                return null;
            }
        }

        public static RotateFlipType DrehungFuerExif(int orientierung)
        {
            return orientierung switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.Rotate180FlipX,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone,
            };
        }
    }
}
