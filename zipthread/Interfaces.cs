namespace zipthread
{
    /// <summary>
    /// интерфейс для работы с очередями
    /// </summary>
    public interface IGZipManagerQueue
    {
        // колво порций
        int GetPartCount();
        // текущий индекс прочитанных порций
        int GetReadIndex();
        // текущий индекс записанных порций
        int GetWriteIndex();
        // завершено ли чтение
        bool IsReadDone();
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

    // интерфейс для чтения
    public interface IGZipReader
    {
        bool DoRead();
    }

}
