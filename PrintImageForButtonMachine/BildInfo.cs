using System.Text.Json;

namespace PrintImageForButtonMachine
{
    /// <summary>
    /// Was zu einem Foto gespeichert wird: die einmalig erkannten Gesichter und, falls von Hand bearbeitet, der Ausschnitt.
    /// </summary>
    internal record BildInfo(List<Gesicht> Gesichter, Ausschnitt? Manuell)
    {
        // Stand des Fotos beim Speichern; ändert sich das Foto (z. B. noch im Schreibvorgang), gilt die Begleitdatei nicht mehr
        public long FotoLaenge { get; init; }
        public long FotoGeaendertUtc { get; init; }
    }

    /// <summary>
    /// Liest und schreibt die Begleitdatei, die neben dem Foto liegt.
    /// </summary>
    internal static class BildInfoSpeicher
    {
        private static readonly JsonSerializerOptions Optionen = new() { WriteIndented = true };

        private enum Zustand { Gueltig, Ungueltig, NichtLesbar }

        // Endet bewusst nicht auf .jpg, damit die Datei nicht als Foto gezählt wird
        public static string PfadFuer(string fotoPfad) => Path.ChangeExtension(fotoPfad, ".button.json");

        /// <returns>null, wenn die Datei fehlt, nicht lesbar ist oder zu einem anderen Stand des Fotos gehört.</returns>
        public static BildInfo? Lade(string fotoPfad)
        {
            (Zustand zustand, BildInfo? info) = Lies(fotoPfad);
            return zustand == Zustand.Gueltig ? info : null;
        }

        /// <summary>
        /// true, wenn das Foto (neu) erkannt werden muss. Ist die Begleitdatei nur gerade gesperrt, wird sie nicht
        /// überschrieben, damit eine Handkorrektur nicht verloren geht.
        /// </summary>
        public static bool BrauchtErkennung(string fotoPfad) => Lies(fotoPfad).Zustand == Zustand.Ungueltig;

        /// <returns>false, wenn das Foto nicht (mehr) existiert, z. B. weil es inzwischen gedruckt und verschoben wurde.</returns>
        public static bool Speichere(string fotoPfad, BildInfo info)
        {
            var foto = new FileInfo(fotoPfad);
            if (!foto.Exists) return false;
            string ziel = PfadFuer(fotoPfad);
            // Erst vollständig schreiben, dann umbenennen, damit nie eine halbe Datei gelesen wird
            string temp = ziel + ".tmp";
            var mitStand = info with { FotoLaenge = foto.Length, FotoGeaendertUtc = foto.LastWriteTimeUtc.Ticks };
            File.WriteAllText(temp, JsonSerializer.Serialize(mitStand, Optionen));
            File.Move(temp, ziel, true);
            return true;
        }

        private static (Zustand Zustand, BildInfo? Info) Lies(string fotoPfad)
        {
            string pfad = PfadFuer(fotoPfad);
            var foto = new FileInfo(fotoPfad);
            if (!File.Exists(pfad) || !foto.Exists) return (Zustand.Ungueltig, null);
            string text;
            try
            {
                text = File.ReadAllText(pfad);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return (Zustand.NichtLesbar, null);
            }
            try
            {
                BildInfo? info = JsonSerializer.Deserialize<BildInfo>(text, Optionen);
                if (info?.Gesichter == null) return (Zustand.Ungueltig, null);
                bool passt = info.FotoLaenge == foto.Length && info.FotoGeaendertUtc == foto.LastWriteTimeUtc.Ticks;
                return passt ? (Zustand.Gueltig, info) : (Zustand.Ungueltig, null);
            }
            catch (JsonException)
            {
                return (Zustand.Ungueltig, null);
            }
        }
    }
}
