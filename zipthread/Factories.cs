using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zipthread
{
    /// <summary>
    /// статичная фабрика создания классов
    /// </summary>
    class Factories
    {
        public static IGZipThread CreateGZipManager(string[] _args)
        {
            return new Manager(_args);
        }

        public static IGZipThread CreateGZipWriter(string FileName, IGZipManagerQueue ComMan)
        {
            return new Writer(FileName, ComMan);
        }

        public static IGZipThread CreateGZipThread(bool _compress, IGZipManagerQueue _queue)
        {
            if (_compress)
            {
                return new CompressorThread(_queue);
            }
            else
            {
                return new DecompressorThread(_queue);
            }
        }

        public static IGZipReader CreateGZipReader(bool _compress, IGZipManagerQueue _queue, int _buffersize, string _filename)
        {
            if (_compress)
            {
                return new CompressorReader(_buffersize, _filename, _queue);
            }
            else
            {
                return new DecompressorReader(_filename, _queue);
            }
        }

    }
}
