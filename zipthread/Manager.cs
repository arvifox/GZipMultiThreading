using System;
using System.Threading;
using System.IO;

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
                if (args[0].Equals(arg_decompress) && !MyType.IsMyFileType(args[1]))
                {
                    resultOk = SimpleDecompressor.Decompress(args[1], args[2]);
                }
                else
                {
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
                reader = Factories.CreateGZipReader(args[0].Equals(arg_compress), QManager, 1024 * 1024 * 4, args[1]);
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
    }
}
