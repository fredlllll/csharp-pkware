using CSPKWare.Exceptions;
using System.IO;

namespace CSPKWare
{
    class Implode
    {
        private byte[] inputBuffer = { };
        private byte[] outputBuffer = { };
        private byte dictionarySizeBits;
        private byte dictionarySizeMask;
        private byte[] nChBits = new byte[0x306];
        private byte[] nChCodes = new byte[0x306];
        private int outBits = 0;

        private void setup(uint compressionType, uint dictionarySize)
        {
            ushort nCount;
            ushort nChCode = 0;

            switch (dictionarySize)
            {
                case (uint)DictionarySize.Large:
                    this.dictionarySizeBits = 6;
                    this.dictionarySizeMask = (byte)Binary.nBitsOfOnes(6);
                    break;
                case (uint)DictionarySize.Medium:
                    this.dictionarySizeBits = 5;
                    this.dictionarySizeMask = (byte)Binary.nBitsOfOnes(5);
                    break;
                case (uint)DictionarySize.Small:
                    this.dictionarySizeBits = 4;
                    this.dictionarySizeMask = (byte)Binary.nBitsOfOnes(4);
                    break;
                default:
                    throw new InvalidDictionarySize(dictionarySize);
            }

            switch (compressionType)
            {
                case (uint)CompressionType.Binary:
                    for (nCount = 0; nCount <= 0xff; nCount++)
                    {
                        this.nChBits[nCount] = 9;
                        this.nChCodes[nCount] = (byte)nChCode;
                        nChCode = (ushort)(Binary.getLowestNBits(16, nChCode) + 2);
                    }
                    break;
                case (uint)CompressionType.Ascii:
                    for (nCount = 0; nCount <= 0xff; nCount++)
                    {
                        this.nChBits[nCount] = (byte)(TablesImplode.ChBitsAsc[nCount] + 1);
                        this.nChCodes[nCount] = (byte)(TablesImplode.ChCodeAsc[nCount] * 2);
                    }
                    break;
                default:
                    throw new InvalidCompressionType(compressionType);
            }

            nCount = 0x100;
            for (int i = 0; i < 0x10; i++)
            {
                for (int nCount2 = 0; nCount2 < 1 << TablesImplode.ExLenBits[i]; nCount2++)
                {
                    this.nChBits[nCount] = (byte)(TablesImplode.ExLenBits[i] + TablesImplode.LenBits[i] + 1);
                    this.nChCodes[nCount] = (byte)(nCount2 << (TablesImplode.LenBits[i] + 1) | (TablesImplode.LenCode[i] * 2) | 1);
                    nCount++;
                }
            }

            this.outputBuffer = new byte[] { (byte)compressionType, this.dictionarySizeBits, 0};
        }

        public Implode(uint compressionType, uint dictionarySize)
        {
            this.setup(compressionType, dictionarySize);
        }
    }
}
