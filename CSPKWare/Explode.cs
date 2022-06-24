using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSPKWare
{
    /// <summary>
    /// translated from https://github.com/arx/ArxLibertatis/blob/master/src/io/Blast.cpp
    /// </summary>
    public class Explode
    {
        public static byte[] DoExplode(byte[] bytes)
        {
            MemoryStream input = new MemoryStream(bytes);
            MemoryStream output = new MemoryStream();

            State s = new State();
            s.inputStream = input;
            s.outputStream = output;

            Blast.blastDecompress(s);

            return output.ToArray();
        }

        const int MAXBITS = 13;      /* maximum code length */
        const int MAXWIN = 4096;             /* maximum window size */

        class State
        {
            public Stream inputStream;
            public int bitBuf = 0; /* bit buffer */
            public int bitCnt = 0; /* number of bits in bit buffer */

            public Stream outputStream;
            public int next = 0; /* index of next write location in out[] */
            public int first = 0;/* true to check distances (for first 4K) */
            public byte[] outBuffer = new byte[MAXWIN]; /* output buffer and sliding window */
        }

        class Huffman
        {
            public short[] count;
            public short[] symbol;
        }

        enum BlastResult
        {
            BLAST_TRUNCATED_INPUT = 2, // ran out of input before completing decompression
            BLAST_OUTPUT_ERROR = 1, // output error before completing decompression
            BLAST_SUCCESS = 0, // successful decompression
            BLAST_INVALID_LITERAL_FLAG = -1, // literal flag not zero or one
            BLAST_INVALID_DIC_SIZE = -2, // dictionary size not in 4..6
            BLAST_INVALID_OFFSET = -3, // distance is too far back
        }




        class Blast
        {
            static int virgin = 1; /* build tables once */
            static short[] litcnt = new short[MAXBITS + 1]; /* litcode memory */
            static short[] litsym = new short[256];
            static short[] lencnt = new short[MAXBITS + 1]; /* lencode memory */
            static short[] lensym = new short[16];
            static short[] distcnt = new short[MAXBITS + 1]; /* distcode memory */
            static short[] distsym = new short[64];

            static Huffman litcode = new Huffman() { count = litcnt, symbol = litsym }; /* length code */
            static Huffman lencode = new Huffman() { count = lencnt, symbol = lensym }; /* length code */
            static Huffman distcode = new Huffman() { count = distcnt, symbol = distsym }; /* distance code */

            /* bit lengths of literal codes */
            static byte[] litlen = new byte[] {
        11, 124, 8, 7, 28, 7, 188, 13, 76, 4, 10, 8, 12, 10, 12, 10, 8, 23, 8,
        9, 7, 6, 7, 8, 7, 6, 55, 8, 23, 24, 12, 11, 7, 9, 11, 12, 6, 7, 22, 5,
        7, 24, 6, 11, 9, 6, 7, 22, 7, 11, 38, 7, 9, 8, 25, 11, 8, 11, 9, 12,
        8, 12, 5, 38, 5, 38, 5, 11, 7, 5, 6, 21, 6, 10, 53, 8, 7, 24, 10, 27,
        44, 253, 253, 253, 252, 252, 252, 13, 12, 45, 12, 45, 12, 61, 12, 45,
        44, 173};

            /* bit lengths of length codes 0..15 */
            static byte[] lenlen = new byte[]
            {
                 2, 35, 36, 53, 38, 23
            };

            /* bit lengths of distance codes 0..63 */
            static byte[] distlen = new byte[]
            {
                2, 20, 53, 230, 247, 151, 248
            };

            /* base for length codes */
            static short[] _base = new short[]
            {
                3, 2, 4, 5, 6, 7, 8, 9, 10, 12, 16, 24, 40, 72, 136, 264
            };

            /* extra bits for length codes */
            static byte[] extra = new byte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8
            };

            /// <summary>
            /// Return need bits from the input stream.This always leaves less than
            /// eight bits in the buffer.bits() works properly for need == 0.
            ///
            /// Format notes:
            ///
            /// - Bits are stored in bytes from the least significant bit to the most
            /// significant bit.Therefore bits are dropped from the bottom of the bit
            /// buffer, using shift right, and new bytes are appended to the top of the
            /// bit buffer, using shift left.
            ///
            /// </summary>
            /// <param name="s"></param>
            /// <param name="need"></param>
            /// <returns></returns>
            static int Bits(State s, int need)
            {

                int val;            /* bit accumulator */

                /* load at least need bits into val */
                val = s.bitBuf;
                while (s.bitCnt < need)
                {
                    int b = s.inputStream.ReadByte();
                    if (b < 0)
                    {
                        throw new Exception("out of input");
                    }
                    val |= b << s.bitCnt; /* load eight bits */
                    s.bitCnt += 8;
                }

                /* drop need bits and update buffer, always zero to seven bits left */
                s.bitBuf = val >> need;
                s.bitCnt -= need;

                /* return need bits, zeroing the bits above that */
                return val & ((1 << need) - 1);
            }

            static int Construct(Huffman h, byte[] rep)
            {
                int n = rep.Length;
                int repOffset = 0;


                int symbol;         /* current symbol when stepping through length[] */
                int len;            /* current length when stepping through h->count[] */
                int left;           /* number of possible codes left of current length */
                short[] offs = new short[MAXBITS + 1];      /* offsets in symbol table for each length */
                short[] length = new short[256];  /* code lengths */

                /* convert compact repeat counts into symbol bit length list */
                symbol = 0;
                do
                {
                    len = rep[repOffset++];
                    left = (len >> 4) + 1;
                    len &= 15;
                    do
                    {
                        length[symbol++] = (short)len;
                    } while (--left != 0);
                } while (--n != 0);
                n = symbol;

                /* count number of codes of each length */
                for (len = 0; len <= MAXBITS; len++)
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
                for (len = 1; len <= MAXBITS; len++)
                {
                    left <<= 1;                     /* one more bit, double codes left */
                    left -= h.count[len];          /* deduct count from possible codes */
                    if (left < 0) return left;      /* over-subscribed--return negative */
                }                                   /* left > 0 means incomplete */

                /* generate offsets into symbol table for each length for sorting */
                offs[1] = 0;
                for (len = 1; len < MAXBITS; len++)
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

            static int Decode(State s, Huffman h)
            {

                int len;            /* current number of bits in code */
                int code;           /* len bits being decoded */
                int first;          /* first code of length len */
                int count;          /* number of codes of length len */
                int index;          /* index of first code of length len in symbol table */
                int bitbuf;         /* bits from stream */
                int left;           /* bits left in next or left to process */
                int countPointer;

                bitbuf = s.bitBuf;
                left = s.bitCnt;
                code = first = index = 0;
                len = 1;
                countPointer = 1;
                while (true)
                {
                    while (left-- != 0)
                    {
                        code |= (bitbuf & 1) ^ 1;   /* invert code */
                        bitbuf >>= 1;
                        count = h.count[countPointer++];
                        if (code < first + count)
                        { /* if length len, return symbol */
                            s.bitBuf = bitbuf;
                            s.bitCnt = (s.bitCnt - len) & 7;
                            return h.symbol[index + (code - first)];
                        }
                        index += count;             /* else update for next length */
                        first += count;
                        first <<= 1;
                        code <<= 1;
                        len++;
                    }
                    left = (MAXBITS + 1) - len;
                    if (left == 0) break;
                    bitbuf = s.inputStream.ReadByte();
                    if (bitbuf < 0)
                    {
                        throw new Exception("out of input");
                    }
                    if (left > 8) left = 8;
                }
                return -9;                          /* ran out of codes */
            }

            public static BlastResult blastDecompress(State s)
            {

                int lit;            /* true if literals are coded */
                int dict;           /* log2(dictionary size) - 6 */
                int symbol;         /* decoded symbol, extra bits for distance */
                int len;            /* length for copy */
                int dist;           /* distance for copy */
                int copy;           /* copy counter */
                int fromPointer, toPointer;
                //unsigned char* from, *to;   /* copy pointers */

                /* set up decoding tables (once--might not be thread-safe) */
                if (virgin != 0)
                {
                    Construct(litcode, litlen);
                    Construct(lencode, lenlen);
                    Construct(distcode, distlen);
                    virgin = 0;
                }

                /* read header */
                lit = Bits(s, 8);
                if (lit > 1)
                {
                    return BlastResult.BLAST_INVALID_LITERAL_FLAG;
                }
                dict = Bits(s, 8);
                if (dict < 4 || dict > 6)
                {
                    return BlastResult.BLAST_INVALID_DIC_SIZE;
                }

                /* decode literals and length/distance pairs */
                do
                {
                    if (Bits(s, 1) != 0)
                    {
                        /* get length */
                        symbol = Decode(s, lencode);
                        len = _base[symbol] + Bits(s, extra[symbol]);
                        if (len == 519) break;              /* end code */

                        /* get distance */
                        symbol = len == 2 ? 2 : dict;
                        dist = Decode(s, distcode) << symbol;
                        dist += Bits(s, symbol);
                        dist++;
                        if (s.first != 0 && dist > s.next)
                        {
                            return BlastResult.BLAST_INVALID_OFFSET;
                        }

                        /* copy length bytes from distance bytes back */
                        do
                        {
                            toPointer = s.next; //points into s.outBuffer
                            fromPointer = toPointer - dist;
                            copy = MAXWIN;
                            if (s.next < dist)
                            {
                                fromPointer += copy;
                                copy = dist;
                            }
                            copy -= s.next;
                            if (copy > len) copy = len;
                            len -= copy;
                            s.next += copy;
                            do
                            {
                                s.outBuffer[toPointer++] = s.outBuffer[fromPointer++];
                            } while (--copy != 0);
                            if (s.next == MAXWIN)
                            {
                                s.outputStream.Write(s.outBuffer, 0, MAXWIN);
                                s.next = 0;
                                s.first = 0;
                            }
                        } while (len != 0);

                    }
                    else
                    {
                        /* get literal and write it */
                        symbol = lit != 0 ? Decode(s, litcode) : Bits(s, 8);
                        s.outBuffer[s.next++] = (byte)symbol;
                        if (s.next == MAXWIN)
                        {
                            s.outputStream.Write(s.outBuffer, 0, MAXWIN);
                            s.next = 0;
                            s.first = 0;
                        }
                    }
                } while (true);

                s.outputStream.Write(s.outBuffer, 0, s.next);

                return BlastResult.BLAST_SUCCESS;
            }
        }
    }
}
