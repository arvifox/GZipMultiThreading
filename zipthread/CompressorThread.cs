using System;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace zipthread
{
    /// <summary>
    /// класс, сжимающий порции в потоках
    /// </summary>
    class CompressorThread : IGZipThread
    {
        private Thread cthr;
        private int tnumber;
        private byte[] indata;
        private byte[] outdata;
        // интерфейс для доступа к очереди порций данных
        private IGZipManagerQueue gzipqueue;
        private bool isDone = false;
        private bool Ok = true;

        public CompressorThread(IGZipManagerQueue _queue, int _number)
        {
            cthr = new Thread(this.run);
            tnumber = _number;
            gzipqueue = _queue;
            cthr.Name = "compressorthread";
        }

        void run()
        {
            try
            {
                // пока есть что сжимать
                while ((indata = gzipqueue.GetRead(tnumber)) != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                        {
                            gz.Write(indata, 0, indata.Length);
                        }
                        outdata = ms.ToArray();
                    }
                    // перед порцией данных запишем 4 байта размер этой порции
                    int totallenght = outdata.Length + 4;
                    Array.Resize<byte>(ref outdata, totallenght);
                    Array.Copy(outdata, 0, outdata, 4, totallenght - 4);
                    outdata[0] = 0;
                    outdata[1] = 0;
                    outdata[2] = 0;
                    outdata[3] = 0;
                    BitConverter.GetBytes(totallenght - 4).CopyTo(outdata, 0);
                    // отдаем сжатые данные в другую очередь по порядку
                    gzipqueue.PutWrite(tnumber, outdata);
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
