using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

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
            // в файле в разделе FEXTRA считывает размер куска файла для чтения и отдает в отдельный поток для распаковки
            int xlen = 0;
            byte[] bytestoread;
            try
            {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                {
                    int dsize = 0;
                    while (file_in.Position < file_in.Length)
                    {
                        // read part of file_in
                        bytestoread = new byte[12];
                        file_in.Read(bytestoread, 0, 12);
                        xlen = BitConverter.ToUInt16(bytestoread, 10);
                        Array.Resize<byte>(ref bytestoread, bytestoread.Length + xlen);
                        file_in.Read(bytestoread, 12, xlen);
                        int j = 12;
                        while (j < xlen + 12)
                        {
                            if (bytestoread[j] == 1 && bytestoread[j + 1] == 1)
                            {
                                dsize = BitConverter.ToInt32(bytestoread, j + 4);
                                break;
                            }
                            j = BitConverter.ToUInt16(bytestoread, j + 2) + 4;
                        }
                        Array.Resize<byte>(ref bytestoread, dsize);
                        file_in.Read(bytestoread, 12 + xlen, dsize - (12 + xlen));
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
