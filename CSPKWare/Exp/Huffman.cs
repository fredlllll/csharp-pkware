using System;
using System.Collections.Generic;
using System.Text;

namespace CSPKWare.Exp
{
    public class Huffman
    {
        public readonly short[] count;
        public readonly short[] symbol;
        public readonly byte[] len;

        public Huffman(short[] count, short[] symbol, byte[] len)
        {
            this.count = count;
            this.symbol = symbol;
            this.len = len;
        }
    }
}
