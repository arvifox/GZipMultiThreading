using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace zipthread
{
    /// <summary>
    /// класс для распаковки в потоках
    /// </summary>
    class DecompressorThread : IGZipThread
    {
        private Thread cthr;
        private byte[] indata;
        private byte[] outdata;
        private int position = 0;
        // интерфейс для доступа к очереди порций данных
        private IGZipManagerQueue gzipqueue;
        private bool isDone = false;

        public DecompressorThread(IGZipManagerQueue _queue)
        {
            cthr = new Thread(this.run);
            gzipqueue = _queue;
            cthr.Name = "decompressorthread";
        }

        void run()
        {
            // пока есть что распаковывать
            while (!gzipqueue.IsReadDone() || gzipqueue.GetReadIndex() < gzipqueue.GetPartCount())
            {
                indata = null;
                indata = gzipqueue.GetRead(ref position);
                if (indata != null)
                {
                    using (GZipStream gz = new GZipStream(new MemoryStream(indata), CompressionMode.Decompress))
                    {
                        int bsize = 4096;
                        byte[] b = new byte[bsize];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int memc = 0;
                            do
                            {
                                memc = gz.Read(b, 0, bsize);
                                if (memc > 0)
                                {
                                    ms.Write(b, 0, memc);
                                }
                            } while (memc > 0);
                            outdata = ms.ToArray();
                        }
                    }
                    // запись порции в очередь готовых
                    gzipqueue.PutWrite(position, outdata);
                }
            }
            isDone = true;
        }

        /// <summary>
        /// реализация интерфейса
        /// </summary>

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
            return true;
        }

        public bool IsDone()
        {
            return isDone;
        }
    }
}
