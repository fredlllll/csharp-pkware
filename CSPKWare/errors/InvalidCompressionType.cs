using System;

namespace csharp_pkware.errors
{
    class InvalidCompressionType : Exception
    {
        public InvalidCompressionType()
        {
        }

        public InvalidCompressionType(uint compressionType) : base($"Invalid Compression Type {compressionType}")
        {
        }
    }
}
