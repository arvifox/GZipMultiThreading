using System;
using System.IO;
using System.IO.Compression;

namespace zipthread
{
    class SimpleDecompressor
    {
        public static bool Decompress(string sfilein, string sfileout)
        {
            // простое расжатие в один поток
            try
            {
                /// распаковка в один поток
                using (FileStream file_in = new FileStream(sfilein, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream file_out = new FileStream(sfileout, FileMode.Create, FileAccess.Write))
                    {
                        using (GZipStream gz = new GZipStream(file_in, CompressionMode.Decompress))
                        {
                            Byte[] buffer = new Byte[4096];
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
}
