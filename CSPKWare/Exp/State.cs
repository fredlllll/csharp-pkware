using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSPKWare.Exp
{
    public class State
    {
        public Stream inputStream;
        public int bitBuf = 0; /* bit buffer */
        public int bitCnt = 0; /* number of bits in bit buffer */

        public Stream outputStream;
        public int next = 0; /* index of next write location in out[] */
        public int first = 0;/* true to check distances (for first 4K) */
        public byte[] outBuffer = new byte[Constants.MAXWIN]; /* output buffer and sliding window */
    }
}
