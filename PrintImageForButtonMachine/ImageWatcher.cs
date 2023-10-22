using PrintImageForButtonMachine;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    internal class ImageWatcher
    {
        private FileSystemWatcher _watcher;
        private ImageProcessor _processor;

        public FileSystemWatcher Watcher { get => _watcher;}

        public ImageWatcher(ImageProcessor p_Processor)
        {
            _processor = p_Processor;
            _watcher = new FileSystemWatcher();
        }

        public void StarteFileSystemWatcher() {
            

            if (string.IsNullOrWhiteSpace(_processor.UeberwachungPfad))
            {
                MessageBox.Show("Es wurde kein Ordner zum Überwachen angegeben", "Unvollständige Angaben", MessageBoxButtons.OK);
                return;
            }

            if (string.IsNullOrWhiteSpace(_processor.AusgabePfad))
            {
                MessageBox.Show("Es wurde kein Ausgabeordner angegeben", "Unvollständige Angaben", MessageBoxButtons.OK);
                return;
            }

            if (_processor.AusgabePfad == _processor.UeberwachungPfad)
            {
                MessageBox.Show("Überwachter Ordner und Ausgabeordner müssen unterschiedliche sein", "Unterschiedliche Ordner auswählen", MessageBoxButtons.OK);
                return;
            }

            _watcher = new FileSystemWatcher();
            _watcher.Path = _processor.UeberwachungPfad;
            _watcher.Filter = "*.jpg";
            _watcher.Created += new FileSystemEventHandler(OnChanged);
            _watcher.EnableRaisingEvents = true;
        }

        public void StoppeFileSystemWatcher() { 
            _watcher.EnableRaisingEvents = false;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            DirectoryInfo directory = new DirectoryInfo(_watcher.Path);
            FileInfo[] files = directory.GetFiles("*.jpg");

            if (files.Length >= _processor.ErmittleAnzahlBilderProSeite())
            {
                _processor.BearbeiteOrdner();
            }
            else {
                _processor.GeneriereVorschau();
            }
        }
    }
}
