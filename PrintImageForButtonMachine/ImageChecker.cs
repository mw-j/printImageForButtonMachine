using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintImageForButtonMachine
{
    internal class ImageChecker
    {
        private readonly ImageProcessor _processor;
        private CancellationTokenSource? _cts;

        public ImageChecker(ImageProcessor p_Processor)
        {
            _processor = p_Processor;
        }

        /// <summary>
        /// Wird ausgelöst, wenn die Überwachung wegen eines Fehlers beendet wurde.
        /// </summary>
        public event EventHandler? Gestoppt;

        public bool DoCheck { get => _cts != null; }

        /// <returns>false, wenn die Einstellungen ungültig sind und die Überwachung nicht gestartet wurde.</returns>
        public bool startCheck() {
            if (_cts != null) return true;
            if (!PruefeEinstellungen()) return false;

            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts);
            return true;
        }

        internal void stopCheck()
        {
            // Bricht auch ein laufendes Task.Delay sofort ab, sodass bei schnellem Neustart keine zweite Schleife entsteht
            _cts?.Cancel();
            _cts = null;
        }

        private async Task RunAsync(CancellationTokenSource cts) {
            CancellationToken token = cts.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await _processor.PruefeOrdnerAsync();
                    await Task.Delay(2000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Überwachung wurde beendet
            }
            catch (Exception ex)
            {
                if (_cts == cts)
                {
                    stopCheck();
                    Gestoppt?.Invoke(this, EventArgs.Empty);
                }
                MessageBox.Show($"Die Überwachung wurde wegen eines Fehlers beendet:\n{ex.Message}", "Fehler bei der Überwachung", MessageBoxButtons.OK);
            }
            finally
            {
                cts.Dispose();
            }
        }

        private bool PruefeEinstellungen()
        {
            if (string.IsNullOrWhiteSpace(_processor.UeberwachungPfad) || !Directory.Exists(_processor.UeberwachungPfad))
            {
                MessageBox.Show("Es wurde kein gültiger Ordner zum Überwachen angegeben", "Unvollständige Angaben", MessageBoxButtons.OK);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_processor.AusgabePfad))
            {
                MessageBox.Show("Es wurde kein Ausgabeordner angegeben", "Unvollständige Angaben", MessageBoxButtons.OK);
                return false;
            }

            string ueberwachung = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_processor.UeberwachungPfad));
            string ausgabe = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_processor.AusgabePfad));
            if (string.Equals(ueberwachung, ausgabe, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Überwachter Ordner und Ausgabeordner müssen unterschiedliche sein", "Unterschiedliche Ordner auswählen", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }
    }
}
