using System;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace zipthread
{
    // класс, выполняющий синхронизацию между потоками чтения, записи и упаковки/распаковки данных
    class Manager : IGZipThread
    {
        private const string arg_compress = "compress";
        private const string arg_decompress = "decompress";

        /// <summary>
        /// работа производится в отдельном потоке
        /// </summary>
        private Thread thr;

        /// <summary>
        /// параметры командной строки
        /// </summary>
        private string[] args;

        /// <summary>
        /// результат работы класса
        /// </summary>
        public bool resultOk { get; set; }

        /// <summary>
        /// флаг, показывающий завершил ли класс свою работу
        /// </summary>
        public bool isDone { get; set; }

        // массив потоков
        private IGZipThread[] threads;

        // колво потоков
        private int threadscount = 0;

        // поток пишущий в файл
        private IGZipThread writer = null;

        // класс, читающий из файла
        private IGZipReader reader;

        // объект, управляющий очередями
        private IGZipManagerQueue QManager;

        /// <summary>
        /// конструктор
        /// </summary>
        public Manager(string[] _args)
        {
            args = _args;
            thr = new Thread(this.parse);
            threadscount = Environment.ProcessorCount;
            threads = new IGZipThread[threadscount];
            QManager = Factories.CreateQueueManager(threadscount);
        }

        /// <summary>
        /// метод, выполняющийся в потоке
        /// </summary>
        private void parse()
        {
            // если 3 параметра командной строки И первый - compress или decompress И второй - существующий файл
            if ((args.Length == 3) && File.Exists(args[1]) && ((args[0].Equals(arg_compress)) || (args[0].Equals(arg_decompress))))
            {
                if (args[0].Equals(arg_decompress) && !IsMyFileType())
                {
                    // простая распаковка в один поток 
                    resultOk = SimpleDecompress();
                }
                else
                {
                    // распаковка моих файлов
                    resultOk = GZipCompress();
                }
            }
            else
            {
                resultOk = false;
            }
            isDone = true;
        }

        private bool GZipCompress()
        {
            try
            {
                bool res;
                // запуск пишущего потока
                writer = Factories.CreateGZipWriter(args[2], QManager);
                writer.StartThread();
                for (int i = 0; i < threadscount; i++)
                {
                    // запуск рабочих потокв
                    threads[i] = Factories.CreateGZipThread(args[0].Equals(arg_compress), QManager, i);
                    threads[i].StartThread();
                }
                // запуск чтения
                reader = Factories.CreateGZipReader(args[0].Equals(arg_compress), QManager, 1024 * 1024 * 5, args[1]);
                res = reader.DoRead();
                QManager.ReadDone();
                // ждем пишущий
                writer.JoinThread();
                res = writer.ResultOK();
                return res;
            }
            catch
            {
                /// прерывание всех потоков
                writer.AbortThread();
                for (int j = 0; j < threadscount; j++)
                {
                    if (threads[j] != null)
                    {
                        threads[j].AbortThread();
                        threads[j].JoinThread();
                    }
                }
                writer.JoinThread();
                return false;
            }
        }



        public void StartThread()
        {
            thr.Start();
        }

        public void AbortThread()
        {
            thr.Abort();
        }

        public void JoinThread()
        {
            thr.Join();
        }

        public bool ResultOK()
        {
            return resultOk;
        }

        public bool IsDone()
        {
            return isDone;
        }

        // простое расжатие в один поток
        private bool SimpleDecompress()
        {
            try
            {
                /// распаковка в один поток
                using (FileStream file_in = new FileStream(args[1], FileMode.Open, FileAccess.Read))
                {
                    using (FileStream file_out = new FileStream(args[2], FileMode.Create, FileAccess.Write))
                    {
                        using (GZipStream gz = new GZipStream(file_in, CompressionMode.Decompress))
                        {
                            Byte[] buffer = new Byte[4096];
                            int h;
                            while ((h = gz.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                file_out.Write(buffer, 0, h);
                            }
                            gz.Flush();
                        }
                    }
                }
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// определяет файл упакован этой программой или нет; 
        private bool IsMyFileType()
        {
            bool res = false;
            try
            {
                using (FileStream file_in = new FileStream(args[1], FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[10];
                    int count = 0;
                    count = file_in.Read(buffer, 0, 10);
                    if (count == 10)
                    {
                        if ((buffer[3] & 4) == 4)
                        {
                            count = file_in.Read(buffer, 0, 2);
                            if (count == 2)
                            {
                                int xlen = BitConverter.ToUInt16(buffer, 0);
                                buffer = new byte[xlen];
                                count = file_in.Read(buffer, 0, xlen);
                                if (count == xlen)
                                {
                                    int si1 = 0;
                                    while (si1 < xlen)
                                    {
                                        /// в разделе FEXTRA ищет нужные байты
                                        if (buffer[si1] == 1 && buffer[si1 + 1] == 1)
                                        {
                                            res = true;
                                            break;
                                        }
                                        si1 = BitConverter.ToUInt16(buffer, 2) + 4;
                                    }
                                }
                            }

                        }
                    }
                }
                return res;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
