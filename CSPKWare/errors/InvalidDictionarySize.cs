using System;

namespace csharp_pkware.errors
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
