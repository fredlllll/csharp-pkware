using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace csharp_pkware_test
{
    [TestClass]
    public class Implode
    {
        [TestMethod]
        public void IsAClass()
        {
            var TestData = new byte[] { 0x01, 0x02 };
            Assert.ThrowsException<NotImplementedException>(() => csharp_pkware.PKWare.Explode(TestData));
        }
    }
}
