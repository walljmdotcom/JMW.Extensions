using NUnit.Framework;
using System;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class DateAndTimeTests
    {
        [Test]
        public void GetElapsedTimeTest()
        {
            Assert.That(new DateTime(2018, 01, 01, 01, 01, 01).GetElapsedTime(new DateTime(2018, 1, 1)), Is.EqualTo("01:01:01"));
        }
    }
}