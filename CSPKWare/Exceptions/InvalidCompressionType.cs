using System;

namespace CSPKWare.Exceptions
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
