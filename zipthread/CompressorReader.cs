﻿using System.IO;
using System.Threading;

namespace zipthread
{
    /// <summary>
    /// класс, читающий исходный файл порциями и отдающий эти порции в очередь
    /// </summary>
    class CompressorReader : IGZipThread
    {
        private Thread cthr;

        private int buffersize = 0;
        private string filename_in;

        private bool ok = false;
        private bool done = false;

        /// <summary>
        /// интерфейс для работы с очередью
        /// </summary>
        private IGZipManagerQueue gzipQueue;

        /// <summary>
        /// конструктор
        /// </summary>
        public CompressorReader(int rwbuffersize, string _filename_in, IGZipManagerQueue _gzipQueue)
        {
            cthr = new Thread(this.DoRead);
            buffersize = rwbuffersize;
            filename_in = _filename_in;
            gzipQueue = _gzipQueue;
        }

        public void StartThread()
        {
            cthr.Start();
        }

        public void AbortThread()
        {
            cthr.Abort();
        }

        public void JoinThread()
        {
            cthr.Join();
        }

        public bool ResultOK()
        {
            return ok;
        }

        public bool IsDone()
        {
            return done;
        }

        public void DoRead()
        {
            byte[] bytestoread;
            /// чтение исходного файла кусками; каждый кусок в отдельном потоке сжимается
            try
            {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                {
                    int dsize = 0;
                    while (file_in.Position < file_in.Length)
                    {
                        if (file_in.Length - file_in.Position <= buffersize)
                        {
                            dsize = (int)(file_in.Length - file_in.Position);
                        }
                        else
                        {
                            dsize = buffersize;
                        }
                        bytestoread = new byte[dsize];
                        file_in.Read(bytestoread, 0, dsize);
                        // отдаем порцию в очередь
                        gzipQueue.PutRead(bytestoread);
                    }
                }
                done = true;
                ok = true;
                gzipQueue.ReadDone();
                gzipQueue.Done(true);
            }
            catch
            {
                done = true;
                ok = false;
                gzipQueue.Done(false);
            }
        }
    }
}
