using System.Text.Json;

namespace PrintImageForButtonMachine
{
    /// <summary>
    /// Was zu einem Foto gespeichert wird: die einmalig erkannten Gesichter und, falls von Hand bearbeitet, der Ausschnitt.
    /// </summary>
    internal record BildInfo(List<Gesicht> Gesichter, Ausschnitt? Manuell);

    /// <summary>
    /// Liest und schreibt die Begleitdatei, die neben dem Foto liegt.
    /// </summary>
    internal static class BildInfoSpeicher
    {
        private static readonly JsonSerializerOptions Optionen = new() { WriteIndented = true };

        // Endet bewusst nicht auf .jpg, damit die Datei nicht als Foto gezählt wird
        public static string PfadFuer(string fotoPfad) => Path.ChangeExtension(fotoPfad, ".button.json");

        /// <returns>null, wenn die Datei fehlt oder nicht lesbar ist; das Foto wird dann neu erkannt.</returns>
        public static BildInfo? Lade(string fotoPfad)
        {
            try
            {
                BildInfo? info = JsonSerializer.Deserialize<BildInfo>(File.ReadAllText(PfadFuer(fotoPfad)), Optionen);
                return info?.Gesichter == null ? null : info;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                return null;
            }
        }

        /// <returns>false, wenn das Foto nicht (mehr) existiert, z. B. weil es inzwischen gedruckt und verschoben wurde.</returns>
        public static bool Speichere(string fotoPfad, BildInfo info)
        {
            if (!File.Exists(fotoPfad)) return false;
            string ziel = PfadFuer(fotoPfad);
            // Erst vollständig schreiben, dann umbenennen, damit nie eine halbe Datei gelesen wird
            string temp = ziel + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(info, Optionen));
            File.Move(temp, ziel, true);
            return true;
        }
    }
}
