using System.Text;

namespace DependencyUpdater
{
    public class EncodingUtil
    {
        /// <remarks>http://www.unicode.org/faq/utf_bom.html</remarks>
        public static Encoding Detect(byte[] buffer, int currentBufferLength, out byte[] bom)
        {
            if (currentBufferLength == 0)
            {
                //File is zero length - pick something
                bom = new byte[0];
                return Encoding.UTF8;
            }

            if (currentBufferLength >= 4)
            {
                if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
                {
                    //Big endian UTF-32
                    bom = new byte[] { 0x00, 0x00, 0xFE, 0xFF };
                    return Encoding.GetEncoding(12001);
                }

                if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                {
                    //Little endian UTF-32
                    bom = new byte[] { 0xFF, 0xFE, 0x00, 0x00 };
                    return Encoding.UTF32;
                }
            }

            if (currentBufferLength >= 3)
            {
                if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                {
                    //UTF-8
                    bom = new byte[] { 0xEF, 0xBB, 0xBF };
                    return Encoding.UTF8;
                }
            }

            if (currentBufferLength >= 2)
            {
                if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                {
                    //Big endian UTF-16
                    bom = new byte[] { 0xFE, 0xFF };
                    return Encoding.BigEndianUnicode;
                }

                if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                {
                    //Little endian UTF-16
                    bom = new byte[] { 0xFF, 0xFE };
                    return Encoding.Unicode;
                }
            }

            //Fallback to UTF-8
            bom = new byte[0];
            return Encoding.UTF8;
        }
    }
}
