using System.Collections.Generic;
using System.Threading;

namespace zipthread
{
    class QueueManager : IGZipManagerQueue
    {
        // очередь номеров потоков
        // нужна для синхронизации порций в основных очередях данных
        private Queue<int> queueThreadNumber;

        // очередь для прочитанных порций данных
        private Queue<byte[]> queueRead;

        // очередь и мьютекс для готовых к записи данных
        private Queue<byte[]> queueWrite;

        // событие: queueRead опустела - нужно помещать новые элементы
        private ManualResetEvent queueReadNeed;

        // в queueRead есть элементы - можно брать в потоки на обработку
        private ManualResetEvent queueReadHas;

        // в queueWrite есть элементы - можно брать в поток на запись
        private ManualResetEvent queueWriteHas;

        // массив событий на запись в очередь для потоков
        private ManualResetEvent[] threadsWrite;

        // колво потоков
        private int ThreadsCount = 0;

        // колво элементов в очереди
        private int QueueCount = 0;

        // индексы текущего чтения/записи в очередях
        private int readindex = 0;
        private int writeindex = 0;
        // колво частей которыми читается файл
        private int partCount = 0;

        // закончено ли чтение исходного файла
        private bool isReadDone = false;

        // все потоки закончили работу или кто-то упал
        private ManualResetEvent allFinished;
        // результат работы потоков
        private bool ThreadsSuccess = false;
        // кол-во потоков, сообщивших о своем завершении
        private int ThreadsCountDone = 0;

        public QueueManager(int _threadscount)
        {
            ThreadsCount = _threadscount;
            QueueCount = ThreadsCount * 2;
            queueRead = new Queue<byte[]>();
            queueWrite = new Queue<byte[]>();
            queueThreadNumber = new Queue<int>();
            queueReadNeed = new ManualResetEvent(true);
            queueReadHas = new ManualResetEvent(false);
            queueWriteHas = new ManualResetEvent(false);
            allFinished = new ManualResetEvent(false);
            threadsWrite = new ManualResetEvent[ThreadsCount];
            for (int i = 0; i < ThreadsCount; i++)
            {
                threadsWrite[i] = new ManualResetEvent(false);
            }
        }

        public bool Wait()
        {
            allFinished.WaitOne();
            return ThreadsSuccess;
        }

        public void Done(bool resultOK)
        {
            lock (allFinished)
            {
                if (!resultOK)
                {
                    ThreadsSuccess = false;
                    allFinished.Set();
                }
                else
                {
                    ThreadsCountDone++;
                    /// +2 - read and write threads
                    if (ThreadsCountDone == ThreadsCount + 2)
                    {
                        ThreadsSuccess = true;
                        allFinished.Set();
                    }
                }
            }
        }

        // положить порцию в очередь готовых для сжатия/расжатия
        public void PutRead(byte[] _data)
        {
            // ждем
            queueReadNeed.WaitOne();
            lock (queueRead)
            {
                queueRead.Enqueue(_data);
                partCount++;
                CheckQueueReadEvent();
            }
        }

        // положить порцию в очередь готовых для записи по порядку
        public void PutWrite(int _number, byte[] _data)
        {
            // поток ждет разрешения именно для себя
            // тк порции нужно писать в файл в том же порядке, в котором они считывались
            threadsWrite[_number].WaitOne();
            lock (queueWrite)
            {
                threadsWrite[_number].Reset();
                queueWrite.Enqueue(_data);
                CheckQueueWriteEvent();
                CheckQueueThreadEvent();
            }
        }

        // взять порцию и ее номер из очереди прочитанных
        public byte[] GetRead(int _number)
        {
            byte[] data = null;
            // потоки ждут появления в очереди данных,
            // сделаем lock, чтобы все потоки не кинулись на одну порцию данных
            lock (queueReadHas)
            {
                if (!isReadDone || readindex < partCount)
                {
                    queueReadHas.WaitOne();
                    lock (queueRead)
                    {
                        readindex++;
                        data = queueRead.Dequeue();
                        CheckQueueReadEvent();
                        CheckQueueThreadEvent(_number);
                    }
                }
            }
            return data;
        }

        // взять порцию из очереди готовых для записи
        public byte[] GetWrite()
        {
            byte[] data = null;
            if (!isReadDone || writeindex < partCount)
            {
                // ждет данные в очереди на запись
                queueWriteHas.WaitOne();
                lock (queueWrite)
                {
                    writeindex++;
                    data = queueWrite.Dequeue();
                    CheckQueueWriteEvent();
                    CheckQueueThreadEvent();
                }
            }
            return data;
        }

        /// <summary>
        /// переустановка событий в зависимости от наличия данных в очередях
        /// </summary>
        /// <param name="_number"></param>
        private void CheckQueueThreadEvent(int _number = -1)
        {
            lock (queueThreadNumber)
            {
                if (_number != -1)
                {
                    // поток с номером _number взял следующую порцию на обработку
                    queueThreadNumber.Enqueue(_number);
                }
                bool IsSet = false;
                for (int i = 0; i < ThreadsCount; i++)
                {
                    // проверка есть ли установленное событие
                    // можно заменить на ManualResetEventSlim
                    if (threadsWrite[i].WaitOne(0))
                    {
                        IsSet = true;
                        break;
                    }
                }
                // если в очереди на запись данных меньше чем макс.значение И
                // очередь обрабатывающих потоков не пустая И
                // нет установленного события, То
                // устанавливаем событие на запись для следующего потока из очереди
                if (queueWrite.Count < QueueCount && queueThreadNumber.Count > 0 && !IsSet)
                {
                    int number = queueThreadNumber.Dequeue();
                    threadsWrite[number].Set();
                }
            }
        }

        /// <summary>
        /// установка события для очереди на запись
        /// </summary>
        private void CheckQueueWriteEvent()
        {
            if (queueWrite.Count > 0)
            {
                queueWriteHas.Set();
            }
            else
            {
                queueWriteHas.Reset();
            }
        }

        /// <summary>
        /// установка событий для очереди чтения
        /// </summary>
        private void CheckQueueReadEvent()
        {
            if (queueRead.Count >= QueueCount)
            {
                queueReadNeed.Reset();
            }
            else
            {
                queueReadNeed.Set();
            }
            if (queueRead.Count > 0)
            {
                queueReadHas.Set();
            }
            else
            {
                queueReadHas.Reset();
            }
        }

        public void ReadDone()
        {
            isReadDone = true;
        }
    }
}
