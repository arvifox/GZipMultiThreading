using System;
using System.IO;

namespace zipthread
{
    class MyType
    {
        public static bool IsMyFileType(string sfilename)
        {
            bool res = false;
            try
            {
                using (FileStream file_in = new FileStream(sfilename, FileMode.Open, FileAccess.Read))
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
