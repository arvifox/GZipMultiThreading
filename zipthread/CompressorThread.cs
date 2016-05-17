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
                    // into FEXTRA write the size of the portions
                    // 10 bytes
                    int totallenght = outdata.Length + 10;
                    // set FEXTRA flag
                    outdata[3] = (byte)(4 + outdata[3]);
                    // write FEXTRA
                    Array.Resize<byte>(ref outdata, totallenght);
                    Array.Copy(outdata, 10, outdata, 20, totallenght - 20);
                    outdata[10] = 8;
                    outdata[11] = 0;
                    outdata[12] = 1;
                    outdata[13] = 1;
                    outdata[14] = 4;
                    outdata[15] = 0;
                    BitConverter.GetBytes(totallenght).CopyTo(outdata, 16);
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
