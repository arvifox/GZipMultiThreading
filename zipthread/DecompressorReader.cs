using System;
using System.IO;

namespace zipthread
{
    /// <summary>
    /// класс, читающий архив порциями
    /// </summary>
    class DecompressorReader : IGZipReader
    {
        private string filename_in;
        // интерфейс для доступа к очереди
        private IGZipManagerQueue gzipQueue;

        /// <summary>
        /// конструктор
        /// </summary>
        public DecompressorReader(string _filename_in, IGZipManagerQueue _gzipQueue)
        {
            filename_in = _filename_in;
            gzipQueue = _gzipQueue;
        }

        public bool DoRead()
        {
            int xlen = 0;
            byte[] bytestoread;
            try
            {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                {
                    while (file_in.Position < file_in.Length)
                    {
                        // считываем первые 4 байта, где хранится размер расположенной далее сжатой порции данных
                        bytestoread = new byte[4];
                        file_in.Read(bytestoread, 0, 4);
                        xlen = BitConverter.ToInt32(bytestoread, 0);
                        bytestoread = new byte[xlen];
                        // считываем порцию
                        file_in.Read(bytestoread, 0, xlen);
                        // прочитанные данные отдает в очередь
                        gzipQueue.PutRead(bytestoread);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
