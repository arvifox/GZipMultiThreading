namespace zipthread
{
    /// <summary>
    /// статичная фабрика создания классов
    /// </summary>
    class Factories
    {
        // главный управляющий класс
        public static IGZipThread CreateGZipManager(string[] _args)
        {
            return new Manager(_args);
        }

        // класс, управляющий очередями
        public static IGZipManagerQueue CreateQueueManager(int _threadscount)
        {
            return new QueueManager(_threadscount);
        }

        // пишущий поток
        public static IGZipThread CreateGZipWriter(string FileName, IGZipManagerQueue ComMan)
        {
            return new Writer(FileName, ComMan);
        }

        // рабочие потоки
        public static IGZipThread CreateGZipThread(bool _compress, IGZipManagerQueue _queue, int _number)
        {
            if (_compress)
            {
                return new CompressorThread(_queue, _number);
            }
            else
            {
                return new DecompressorThread(_queue, _number);
            }
        }

        // читающий поток
        public static IGZipReader CreateGZipReader(bool _compress, IGZipManagerQueue _queue, int _buffersize, string _filename)
        {
            if (_compress)
            {
                return new CompressorReader(_buffersize, _filename, _queue);
            }
            else
            {
                return new DecompressorReader(_filename, _queue);
            }
        }

    }
}
