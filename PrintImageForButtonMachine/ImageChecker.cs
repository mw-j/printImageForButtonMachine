using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PrintImageForButtonMachine
{
    internal class ImageChecker
    {
        private ImageProcessor _processor;
        private bool _doCheck;

        public ImageChecker(ImageProcessor p_Processor)
        {
            _processor = p_Processor;
        }

        public bool DoCheck { get => _doCheck; }

        public void startCheck() { 
            _doCheck = true;
            run();
        }

        internal void stopCheck()
        {
            _doCheck = false;
        }

        private async void run() {
            while (_doCheck)
            {
                DirectoryInfo directory = new DirectoryInfo(_processor.UeberwachungPfad);
                FileInfo[] files = directory.GetFiles("*.jpg");
                if (files.Length >= _processor.ErmittleAnzahlBilderProSeite())
                {
                    _processor.BearbeiteOrdner();
                }
                else
                {
                    _processor.GeneriereVorschau();
                }

                await Task.Delay(2000);
            }
        }
    }
}
