using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace zipthread
{
    class CompressorManager
    {
        private int threadscount = 0;
        private int buffersize = 0;
        private string filename_in;
        private string filename_out;

        /// <summary>
        /// события для синхронизации чтения и записи
        /// </summary>
        private ManualResetEvent[] mres_read;
        private ManualResetEvent[] mres_write;
        private CThread[] thrds;

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="tc">колво потоков</param>
        /// <param name="rwbuffersize">размер буфера чтения</param>
        /// <param name="_filename_in">имя входного файл</param>
        /// <param name="_filename_out">имя выходного файла</param>
        public CompressorManager(int tc, int rwbuffersize, string _filename_in, string _filename_out)
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
            private int partcount = 0;
            private int partcur = 0;
            FileStream file_out;
            CompressorManager cm;
            public bool resultOK = true;

            public CWriter(string name, int pc, CompressorManager _cm)
            {
                cwt = new Thread(this.run);
                cm = _cm;
                cwt.Name = name;
                partcount = pc;
            }

            void run()
            {
                try
                {
                    using (file_out = new FileStream(cm.filename_out, FileMode.Create, FileAccess.Write))
                    {
                        while (partcur < partcount)
                        {
                            cm.mres_write[partcur % cm.threadscount].WaitOne();
                            cm.mres_write[partcur % cm.threadscount].Reset();
                            file_out.Write(cm.thrds[partcur % cm.threadscount].outdata, 0, cm.thrds[partcur % cm.threadscount].outdata.Length);
                            cm.mres_read[partcur % cm.threadscount].Set();
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
        /// класс, выполняющий упаковку данных в отдельных потоках
        /// </summary>
        private class CThread
        {
            public Thread cthr;
            public byte[] indata;
            public byte[] outdata;
            private CompressorManager cm;

            public CThread(string name, CompressorManager _cm)
            {
                cthr = new Thread(this.run);
                cm = _cm;
                cthr.Name = name;
            }

            void run()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                    {
                        gz.Write(indata, 0, indata.Length);
                    }
                    outdata = ms.ToArray();
                }
                // в раздел FEXTRA дописываем размер кусков
                // add 10 bytes of FEXTRA
                int totallenght = outdata.Length + 10;
                // set FEXTRA flag
                outdata[3] = (byte)(4 + outdata[3]);
                // write FEXTRA
                Array.Resize<byte>(ref outdata, totallenght);
                Array.Copy(outdata, 10, outdata, 20, totallenght - 20);
                outdata[10] = 8;
                outdata[11] = 0;
                outdata[12] = 1;
                outdata[13] = 1;
                outdata[14] = 4;
                outdata[15] = 0;
                BitConverter.GetBytes(totallenght).CopyTo(outdata, 16);
                cm.mres_write[Convert.ToInt32(cthr.Name)].Set();
            }
        }

        public bool Compress()
        {
            /// чтение исходного файла кусками; каждый кусок в отдельном потоке сжимается
            CWriter cw = null;
            try
            {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                {
                    int i = 0;
                    int dsize = 0;
                    cw = new CWriter("writer", (int)(file_in.Length / buffersize) + 1, this);
                    cw.cwt.Start();
                    while (file_in.Position < file_in.Length)
                    {
                        mres_read[i].WaitOne();
                        mres_read[i].Reset();
                        CThread c1 = new CThread(Convert.ToString(i), this);
                        thrds[i] = c1;
                        if (file_in.Length - file_in.Position <= buffersize)
                        {
                            dsize = (int)(file_in.Length - file_in.Position);
                        }
                        else
                        {
                            dsize = buffersize;
                        }
                        c1.indata = new byte[dsize];
                        file_in.Read(c1.indata, 0, dsize);
                        c1.cthr.Start();
                        i = (i + 1) % threadscount;
                    }
                    cw.cwt.Join();
                }
                return cw.resultOK == true;
            }
            catch
            {
                /// прерывание всех потоков
                cw.cwt.Abort();
                for (int j = 0; j < threadscount; j++)
                {
                    if (thrds[j] != null)
                    {
                        thrds[j].cthr.Abort();
                        thrds[j].cthr.Join();
                    }
                }
                cw.cwt.Join();
                return false;
            }
        }
    }
}
