using System;
using System.Collections.Generic;
using System.Text;

namespace CSPKWare.Exp
{
    public static class TablesExplode
    {
        /* bit lengths of literal codes */
        static readonly byte[] litlen = new byte[] {
        11, 124, 8, 7, 28, 7, 188, 13, 76, 4, 10, 8, 12, 10, 12, 10, 8, 23, 8,
        9, 7, 6, 7, 8, 7, 6, 55, 8, 23, 24, 12, 11, 7, 9, 11, 12, 6, 7, 22, 5,
        7, 24, 6, 11, 9, 6, 7, 22, 7, 11, 38, 7, 9, 8, 25, 11, 8, 11, 9, 12,
        8, 12, 5, 38, 5, 38, 5, 11, 7, 5, 6, 21, 6, 10, 53, 8, 7, 24, 10, 27,
        44, 253, 253, 253, 252, 252, 252, 13, 12, 45, 12, 45, 12, 61, 12, 45,
        44, 173};

        /* bit lengths of length codes 0..15 */
        static readonly byte[] lenlen = new byte[]
        {
                 2, 35, 36, 53, 38, 23
        };

        /* bit lengths of distance codes 0..63 */
        static readonly byte[] distlen = new byte[]
        {
                2, 20, 53, 230, 247, 151, 248
        };

        /* base for length codes */
        public static readonly short[] _base = new short[]
        {
                3, 2, 4, 5, 6, 7, 8, 9, 10, 12, 16, 24, 40, 72, 136, 264
        };

        /* extra bits for length codes */
        public static readonly byte[] extra = new byte[]
        {
                0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8
        };

        public static readonly Huffman litcode = new Huffman(new short[Constants.MAXBITS + 1], new short[256], litlen); /* length code */
        public static readonly Huffman lencode = new Huffman(new short[Constants.MAXBITS + 1], new short[16], lenlen); /* length code */
        public static readonly Huffman distcode = new Huffman(new short[Constants.MAXBITS + 1], new short[64], distlen); /* distance code */

        static TablesExplode()
        {
            Construct(litcode);
            Construct(lencode);
            Construct(distcode);
        }

        static int Construct(Huffman h)
        {
            int n = h.len.Length;
            int repOffset = 0;


            int symbol;         /* current symbol when stepping through length[] */
            int len;            /* current length when stepping through h->count[] */
            int left;           /* number of possible codes left of current length */
            short[] offs = new short[Constants.MAXBITS + 1];      /* offsets in symbol table for each length */
            short[] length = new short[256];  /* code lengths */

            /* convert compact repeat counts into symbol bit length list */
            symbol = 0;
            do
            {
                len = h.len[repOffset++];
                left = (len >> 4) + 1;
                len &= 15;
                do
                {
                    length[symbol++] = (short)len;
                } while (--left != 0);
            } while (--n != 0);
            n = symbol;

            /* count number of codes of each length */
            for (len = 0; len <= Constants.MAXBITS; len++)
            {
                h.count[len] = 0;
            }
            for (symbol = 0; symbol < n; symbol++)
            {
                (h.count[length[symbol]])++; /* assumes lengths are within bounds */
            }
            if (h.count[0] == n)
            { /* no codes! */
                return 0; /* complete, but decode() will fail */
            }

            /* check for an over-subscribed or incomplete set of lengths */
            left = 1;                           /* one possible code of zero length */
            for (len = 1; len <= Constants.MAXBITS; len++)
            {
                left <<= 1;                     /* one more bit, double codes left */
                left -= h.count[len];          /* deduct count from possible codes */
                if (left < 0) return left;      /* over-subscribed--return negative */
            }                                   /* left > 0 means incomplete */

            /* generate offsets into symbol table for each length for sorting */
            offs[1] = 0;
            for (len = 1; len < Constants.MAXBITS; len++)
            {
                offs[len + 1] = (short)(offs[len] + h.count[len]);
            }

            /*
             * put symbols in table sorted by length, by symbol order within each
             * length
             */
            for (symbol = 0; symbol < n; symbol++)
            {
                if (length[symbol] != 0)
                {
                    h.symbol[offs[length[symbol]]++] = (short)symbol;
                }
            }

            /* return zero for complete set, positive for incomplete set */
            return left;
        }
    }
}
