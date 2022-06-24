using System;

namespace CSPKWare.Exceptions
{
    public class InvalidDictionarySize : Exception
    {
        public InvalidDictionarySize()
        {
        }

        public InvalidDictionarySize(uint dictionarySize): base($"Invalid Dictionary Size {dictionarySize}")
        {
        }
    }
}
