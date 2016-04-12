using System;
using System.Threading;
using System.IO;

namespace zipthread
{
    // класс, реализующий многопоточную упаковку и распаковку данных
    class GZipManager
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
        /// 
        private string[] args;
        /// <summary>
        /// результат работы класса
        /// </summary>
        public bool resultOk = false;

        /// <summary>
        /// флаг, показывающий завершил ли класс свою работу
        /// </summary>
        public bool isDone = false;

        /// <summary>
        /// стартовать работу класса
        /// </summary>
        public void StartWork()
        {
            thr.Start();
        }

        /// <summary>
        /// прервать работу класса
        /// </summary>
        public void StopWork()
        {
            thr.Abort();
            thr.Join();
        }

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="_args"></param>
        public GZipManager(string[] _args)
        {
            args = _args;
            thr = new Thread(this.parse);
        }

        /// <summary>
        /// метод, выполняющийся в потоке
        /// </summary>
        private void parse()
        {
            if ((args.Length == 3) && File.Exists(args[1]))
            {
                if (args[0].Equals(arg_compress))
                {
                    /// <summary>
                    /// класс, выполняющий упаковку; параметры: кол-во потоков, размер буфера чтения, имена файлов
                    /// </summary>
                    CompressorManager cm = new CompressorManager(Environment.ProcessorCount, 1024 * 1024 * 5, args[1], args[2]);
                    resultOk = cm.Compress();
                }
                else if (args[0].Equals(arg_decompress))
                {
                    /// <summary>
                    /// класс, выполняющий распаковку; параметры: кол-во потоков, размер буфера чтения, имена файлов
                    /// </summary>
                    DecompressorManager dm = new DecompressorManager(Environment.ProcessorCount, 1024 * 1024 * 5, args[1], args[2]);
                    resultOk = dm.Decompress();
                }
                else
                {
                    resultOk = false;
                }
            }
            else
            {
                resultOk = false;
            }
            isDone = true;
        }
    }
}
