using System;
using System.Linq;
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
        private byte[] indata;
        private byte[] outdata;
        private int position = 0;
        // интерфейс для доступа к очереди порций данных
        private IGZipManagerQueue gzipqueue;
        private bool isDone = false;

        public CompressorThread(IGZipManagerQueue _queue)
        {
            cthr = new Thread(this.run);
            gzipqueue = _queue;
            cthr.Name = "compressorthread";
        }

        void run()
        {
            // пока есть что сжимать
            while (!gzipqueue.IsReadDone() || gzipqueue.GetReadIndex() < gzipqueue.GetPartCount())
            {
                indata = null;
                // берем порцию и ее номер
                indata = gzipqueue.GetRead(ref position);
                if (indata != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                        {
                            gz.Write(indata, 0, indata.Length);
                        }
                        outdata = ms.ToArray();
                    }
                    // в раздел FEXTRA дописываем размер кусков для совместимости
                    // add 10 bytes of FEXTRA
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
