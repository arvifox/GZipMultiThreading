namespace zipthread
{
    /// <summary>
    /// интерфейс для работы с очередями
    /// </summary>
    public interface IGZipManagerQueue
    {
        // читающий поток сообщит, что чтение завершено и новых данных больше не будет
        void ReadDone();
        // положить порцию в очередь прочитанных
        void PutRead(byte[] _data);
        // положить порцию в очередь обработанных с указанием номера потока, который обработал для синхронизации
        void PutWrite(int _number, byte[] _data);
        // взять порцию из прочитанных на обработку с указанием номера потока, который будет обрабатывать
        byte[] GetRead(int _number);
        // взять порцию обработанных на запись
        byte[] GetWrite();
        // потоки сообщают менеджеру очередей о своем завершении
        void Done(bool resultOK);
        // ждать потоки
        bool Wait();
    }

    // интерфейс для потока
    public interface IGZipThread
    {
        void StartThread();
        void AbortThread();
        void JoinThread();
        bool ResultOK();
        bool IsDone();
    }
}
