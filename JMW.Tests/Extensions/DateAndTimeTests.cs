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
            Assert.That("01:01:01", Is.EqualTo(new DateTime(2018, 01, 01, 01, 01, 01).GetElapsedTime(new DateTime(2018, 1, 1))));
        }
    }
}