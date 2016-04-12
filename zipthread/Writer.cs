using System.Threading;
using System.IO;
using System;

namespace zipthread
{
    /// <summary>
    /// класс, пишущий в файл
    /// </summary>
    class Writer : IGZipThread
    {
        private bool resultOK;
        private Thread writerthread;
        private FileStream file_out;
        // интерфейс для доступа к очереди данных
        private IGZipManagerQueue gzipwriter;
        private string OutputFileName;
        private bool isDone = false;

        public Writer(string name, IGZipManagerQueue _gzipwriter)
        {
            writerthread = new Thread(this.run);
            gzipwriter = _gzipwriter;
            writerthread.Name = "Writer";
            OutputFileName = name;
        }

        /// <summary>
        /// реализация интерфейса
        /// </summary>
        /// <returns></returns>

        public bool ResultOK()
        {
            return resultOK;
        }

        public void StartThread()
        {
            writerthread.Start();
        }

        public void AbortThread()
        {
            writerthread.Abort();
        }

        public void JoinThread()
        {
            writerthread.Join();
        }

        void run()
        {
            byte[] bytestowrite;
            try
            {
                using (file_out = new FileStream(OutputFileName, FileMode.Create, FileAccess.Write))
                {
                    // пока есть данные
                    while (!gzipwriter.IsReadDone() || gzipwriter.GetWriteIndex() < gzipwriter.GetPartCount())
                    {
                        // получает данные из очереди
                        bytestowrite = gzipwriter.GetWrite();
                        if (bytestowrite != null)
                        {
                            file_out.Write(bytestowrite, 0, bytestowrite.Length);
                        }
                    }
                }
                resultOK = true;
                isDone = true;
            }
            catch (IOException)
            {
                resultOK = false;
                isDone = true;
            }
        }

        public bool IsDone()
        {
            return isDone;
        }
    }
}
