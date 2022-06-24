namespace CSPKWare.Imp
{
    class Binary
    {
        public static int nBitsOfOnes(byte numberOfBits)
        {
            return (1 << numberOfBits) - 1;
        }

        public static int getLowestNBits(byte numberOfBits, int number)
        {
            return number & nBitsOfOnes(numberOfBits);
        }
    }
}
