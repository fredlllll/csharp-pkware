using CSPKWare.Exp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSPKWare.Exp
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

            blastDecompress(s);

            return output.ToArray();
        }

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
                left = (Constants.MAXBITS + 1) - len;
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
                    symbol = Decode(s, TablesExplode.lencode);
                    len = TablesExplode._base[symbol] + Bits(s, TablesExplode.extra[symbol]);
                    if (len == 519) break;              /* end code */

                    /* get distance */
                    symbol = len == 2 ? 2 : dict;
                    dist = Decode(s, TablesExplode.distcode) << symbol;
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
                        copy = Constants.MAXWIN;
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
                        if (s.next == Constants.MAXWIN)
                        {
                            s.outputStream.Write(s.outBuffer, 0, Constants.MAXWIN);
                            s.next = 0;
                            s.first = 0;
                        }
                    } while (len != 0);

                }
                else
                {
                    /* get literal and write it */
                    symbol = lit != 0 ? Decode(s, TablesExplode.litcode) : Bits(s, 8);
                    s.outBuffer[s.next++] = (byte)symbol;
                    if (s.next == Constants.MAXWIN)
                    {
                        s.outputStream.Write(s.outBuffer, 0, Constants.MAXWIN);
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
