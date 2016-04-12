using System.IO;
using System.IO.Compression;
using System.Threading;

namespace zipthread
{
    /// <summary>
    /// класс для распаковки в потоках
    /// </summary>
    class DecompressorThread : IGZipThread
    {
        private Thread cthr;
        private int tnumber;
        private byte[] indata;
        private byte[] outdata;
        // интерфейс для доступа к очереди порций данных
        private IGZipManagerQueue gzipqueue;
        private bool isDone = false;
        private bool Ok = false;

        public DecompressorThread(IGZipManagerQueue _queue, int _number)
        {
            cthr = new Thread(this.run);
            tnumber = _number;
            gzipqueue = _queue;
            cthr.Name = "decompressorthread";
        }

        void run()
        {
            try
            {


                // пока есть что распаковывать
                while (!gzipqueue.IsReadDone() || gzipqueue.GetReadIndex() < gzipqueue.GetPartCount())
                {
                    indata = null;
                    indata = gzipqueue.GetRead(tnumber);
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
                        gzipqueue.PutWrite(tnumber, outdata);
                    }
                }
                Ok = true;
                isDone = true;
            }
            catch
            {
                Ok = false;
                isDone = true;
            }
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
            return Ok;
        }

        public bool IsDone()
        {
            return isDone;
        }
    }
}
