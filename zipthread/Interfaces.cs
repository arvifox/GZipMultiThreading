using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zipthread
{
    /// <summary>
    /// интерфейс для работы с очередями
    /// </summary>
    public interface IGZipManagerQueue
    {
        int GetPartCount();
        int GetReadIndex();
        void PutRead(byte[] _data);
        void PutWrite(int _position, byte[] _data);
        byte[] GetRead(ref int _position);
        byte[] GetWrite();
        bool IsReadDone();
    }

    public interface IGZipThread
    {
        void StartThread();
        void AbortThread();
        void JoinThread();
        bool ResultOK();
        bool IsDone();
    }

    public interface IGZipReader
    {
        bool DoRead();
    }

}
