// -----------------------------------------------------------------------
// <copyright file="IBTest.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NUnit.Framework;

//TODO need to abstract away the ib-csharp client to be able to mock it

namespace QDMSTest
{
    [TestFixture]
    public class IBTest
    {
        [Test]
        public void HistoricalRequestsAreSplitToRespectRequestLimits()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void RealTimeRequestsAreReSentAfterARealTimeDataPacingViolation()
        {
            Assert.IsTrue(false);
        }

        [Test]
        public void HistoricalRequestsAreCorrectlyForwardedToTheIBClient()
        {
            Assert.IsTrue(false);
        }
    }
}