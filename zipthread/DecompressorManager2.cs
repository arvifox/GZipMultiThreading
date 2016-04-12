using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zipthread
{
    class DecompressorManager
    {
        public static bool Decompress(string filename_in, string filename_out)
        {
            try
            {
                using (FileStream file_in = new FileStream(filename_in, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream file_out = new FileStream(filename_out, FileMode.Create, FileAccess.Write))
                    {
                        using (GZipStream gz = new GZipStream(file_in, CompressionMode.Decompress))
                        {
                            Byte[] buffer = new Byte[file_in.Length];
                            int h;
                            while ((h = gz.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                file_out.Write(buffer, 0, h);
                            }
                            gz.Flush();
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}
