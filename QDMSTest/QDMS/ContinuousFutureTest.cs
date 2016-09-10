// -----------------------------------------------------------------------
// <copyright file="ContinuousFutureTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class ContinuousFutureTest
    {
        [Test]
        public void UseMonthSerializesCorrectlyWhenSetToFalse()
        {
            var ms = new MemoryStream();
            var cf = new ContinuousFuture();
            cf.UseDec = false;
            var serialized = MyUtils.ProtoBufSerialize(cf, ms);
            var cf2 = MyUtils.ProtoBufDeserialize<ContinuousFuture>(serialized, ms);
            Assert.AreEqual(false, cf2.UseDec);
        }
    }
}
