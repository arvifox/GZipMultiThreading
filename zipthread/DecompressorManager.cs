using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace zipthread
{
    class DecompressorManager
    {
        private int threadscount = 0;
        private int buffersize = 0;
        private string filename_in;
        private string filename_out;

        /// <summary>
        /// события для синхронизации чтения и записи файлов
        /// </summary>
        private ManualResetEvent[] mres_read;
        private ManualResetEvent[] mres_write;
        private CThread[] thrds;
        private int partcount = 0;
        private bool IsReadDone = false;

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="rwbuffersize"></param>
        /// <param name="_filename_in"></param>
        /// <param name="_filename_out"></param>
        public DecompressorManager(int tc, int rwbuffersize, string _filename_in, string _filename_out)
        {
            threadscount = tc;
            buffersize = rwbuffersize;
            filename_in = _filename_in;
            filename_out = _filename_out;
            mres_read = new ManualResetEvent[threadscount];
            mres_write = new ManualResetEvent[threadscount];
            thrds = new CThread[threadscount];
            for (int i = 0; i < threadscount; i++)
            {
                mres_read[i] = new ManualResetEvent(true);
                mres_write[i] = new ManualResetEvent(false);
            }
        }

        /// <summary>
        /// класс, выполняющий запись в файл в отдельном потоке
        /// </summary>
        private class CWriter
        {
            public Thread cwt;
            private int partcur = 0;
            FileStream file_out;
            DecompressorManager dm;
            public bool resultOK = true;

            public CWriter(string name, DecompressorManager _dm)
            {
                cwt = new Thread(this.run);
                dm = _dm;
                cwt.Name = name;
            }

            void run()
            {
                try
                {
                    using (file_out = new FileStream(dm.filename_out, FileMode.Create, FileAccess.Write))
                    {
                        while (!dm.IsReadDone || partcur < dm.partcount)
                        {
                            dm.mres_write[partcur % dm.threadscount].WaitOne();
                            dm.mres_write[partcur % dm.threadscount].Reset();
                            file_out.Write(dm.thrds[partcur % dm.threadscount].outdata, 0, dm.thrds[partcur % dm.threadscount].outdata.Length);
                            dm.mres_read[partcur % dm.threadscount].Set();
                            partcur++;
                        }
                    }
                }
                catch (IOException)
                {
                    resultOK = false;
                }
            }
        }

        /// <summary>
        /// класс, выполняющий распаковку в отдельных потоках
        /// </summary>
        private class CThread
        {
            public Thread cthr;
            public byte[] indata;
            public byte[] outdata;
            private DecompressorManager dm;

            public CThread(string name, DecompressorManager _dm)
            {
                cthr = new Thread(this.run);
                dm = _dm;
                cthr.Name = name;
            }

            void run()
            {
                using (GZipStream gz = new GZipStream(new MemoryStream(indata), CompressionMode.Decompress))
                {
                    int bsize = 4096;
                    byte[] b = new byte[bsize];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int memc = 0;
                        do
                        {
                            memc = gz.Read(b, 0, bsize);
                            if (memc > 0)
                            {
                                ms.Write(b, 0, memc);
                            }
                        } while (memc > 0);
                        outdata = ms.ToArray();
                    }
                }
                dm.mres_write[Convert.ToInt32(cthr.Name)].Set();
            }
        }

        private bool DecompressMyFile()
        {
            // в файле в разделе FEXTRA считывает размер куска файла для чтения и отдает в отдельный поток для распаковки
            CWriter cw = null;
            int xlen = 0;
            try
            {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                {
                    int i = 0;
                    int dsize = 0;
                    cw = new CWriter("writer", this);
                    cw.cwt.Start();
                    while (file_in.Position < file_in.Length)
                    {
                        mres_read[i].WaitOne();
                        mres_read[i].Reset();
                        CThread c1 = new CThread(Convert.ToString(i), this);
                        thrds[i] = c1;
                        // read part of file_in
                        c1.indata = new byte[12];
                        file_in.Read(c1.indata, 0, 12);
                        xlen = BitConverter.ToUInt16(c1.indata, 10);
                        Array.Resize<byte>(ref c1.indata, c1.indata.Length + xlen);
                        file_in.Read(c1.indata, 12, xlen);
                        int j = 12;
                        while (j < xlen + 12)
                        {
                            if (c1.indata[j] == 1 && c1.indata[j + 1] == 1)
                            {
                                dsize = BitConverter.ToInt32(c1.indata, j + 4);
                                break;
                            }
                            j = BitConverter.ToUInt16(c1.indata, j + 2) + 4;
                        }
                        Array.Resize<byte>(ref c1.indata, dsize);
                        file_in.Read(c1.indata, 12 + xlen, dsize - (12 + xlen));
                        partcount++;
                        c1.cthr.Start();
                        i = (i + 1) % threadscount;
                    }
                    IsReadDone = true;
                    cw.cwt.Join();
                }
                return cw.resultOK == true;
            }
            catch
            {
                // прерываение всех потоков
                cw.cwt.Abort();
                cw.cwt.Join();
                for (int j = 0; j < threadscount; j++)
                {
                    if (thrds[j] != null)
                    {
                        thrds[j].cthr.Abort();
                        thrds[j].cthr.Join();
                    }
                }
                return false;
            }
        }

        public bool Decompress()
        {
            /// определение файл упакован этой же программой или нет
            if (IsMyFileType())
            {
                return DecompressMyFile();
            }
            else
            {
                try
                {
                    /// распаковка в один поток
                    using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                    {
                        using (FileStream file_out = new FileStream(filename_out, FileMode.Create, FileAccess.Write))
                        {
                            using (GZipStream gz = new GZipStream(file_in, CompressionMode.Decompress))
                            {
                                Byte[] buffer = new Byte[buffersize];
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
        }

        /// <summary>
        /// определяет файл упакован этой программой или нет; 
        /// </summary>
        /// <returns>true - упакован этой же программой</returns>
        private bool IsMyFileType()
        {
            bool res = false;
            try {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
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
