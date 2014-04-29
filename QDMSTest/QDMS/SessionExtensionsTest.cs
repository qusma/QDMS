using System;
using NUnit.Framework;
using QDMS;

namespace QDMSTest
{
    [TestFixture]
    public class SessionExtensionsTest
    {
        [Test]
        public void OverlapsReturnsTrueForDailyOverlaps()
        {
            var sundayToWednesday = new InstrumentSession { OpeningDay = DayOfTheWeek.Sunday, ClosingDay = DayOfTheWeek.Wednesday };
            var mondayToTuesday = new InstrumentSession { OpeningDay = DayOfTheWeek.Monday, ClosingDay = DayOfTheWeek.Tuesday };
            var tuesdayToThursday = new InstrumentSession { OpeningDay = DayOfTheWeek.Tuesday, ClosingDay = DayOfTheWeek.Thursday };
            var mondayToFriday = new InstrumentSession { OpeningDay = DayOfTheWeek.Monday, ClosingDay = DayOfTheWeek.Friday };
            var wednesdayToSaturday = new InstrumentSession { OpeningDay = DayOfTheWeek.Wednesday, ClosingDay = DayOfTheWeek.Saturday };

            //completely covered, across weeks
            Assert.IsTrue(sundayToWednesday.Overlaps(mondayToTuesday));
            Assert.IsTrue(mondayToTuesday.Overlaps(sundayToWednesday));

            //completely covered, intraweek
            Assert.IsTrue(tuesdayToThursday.Overlaps(mondayToFriday));
            Assert.IsTrue(mondayToFriday.Overlaps(tuesdayToThursday));

            //partially covered, across weeks
            Assert.IsTrue(sundayToWednesday.Overlaps(tuesdayToThursday));
            Assert.IsTrue(tuesdayToThursday.Overlaps(sundayToWednesday));

            //partially covered, intraweek
            Assert.IsTrue(wednesdayToSaturday.Overlaps(tuesdayToThursday));
            Assert.IsTrue(tuesdayToThursday.Overlaps(wednesdayToSaturday));
        }

        [Test]
        public void OverlapsReturnsFalseForNonOverlappingDailyPeriods()
        {
            var sundayToMonday = new InstrumentSession { OpeningDay = DayOfTheWeek.Sunday, ClosingDay = DayOfTheWeek.Monday };
            var mondayToTuesday = new InstrumentSession { OpeningDay = DayOfTheWeek.Monday, ClosingDay = DayOfTheWeek.Tuesday };
            var wednesdayToSaturday = new InstrumentSession { OpeningDay = DayOfTheWeek.Wednesday, ClosingDay = DayOfTheWeek.Saturday };

            //intraweek
            Assert.IsFalse(wednesdayToSaturday.Overlaps(mondayToTuesday));
            Assert.IsFalse(mondayToTuesday.Overlaps(wednesdayToSaturday));

            //Across weeks
            Assert.IsFalse(sundayToMonday.Overlaps(wednesdayToSaturday));
            Assert.IsFalse(wednesdayToSaturday.Overlaps(sundayToMonday));
        }

        [Test]
        public void OverlapsReturnsTrueForIntradayOverlappingPeriods()
        {
            var m10To12 = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(10,0,0),
                ClosingDay = DayOfTheWeek.Monday,
                ClosingTime = new TimeSpan(12, 0, 0),
            };

            var m8To14 = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingDay = DayOfTheWeek.Monday,
                ClosingTime = new TimeSpan(14, 0, 0),
            };

            var m12To16 = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(12, 0, 0),
                ClosingDay = DayOfTheWeek.Monday,
                ClosingTime = new TimeSpan(16, 0, 0),
            };

            var m12ToT12 = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Monday,
                OpeningTime = new TimeSpan(12, 0, 0),
                ClosingDay = DayOfTheWeek.Tuesday,
                ClosingTime = new TimeSpan(12, 0, 0),
            };

            var s20ToM12 = new InstrumentSession
            {
                OpeningDay = DayOfTheWeek.Sunday,
                OpeningTime = new TimeSpan(20, 0, 0),
                ClosingDay = DayOfTheWeek.Monday,
                ClosingTime = new TimeSpan(12, 0, 0),
            };
            
            //one contains the other
            Assert.IsTrue(m10To12.Overlaps(m8To14));
            Assert.IsTrue(m8To14.Overlaps(m10To12));

            //simple overlap
            Assert.IsTrue(m12To16.Overlaps(m8To14));
            Assert.IsTrue(m8To14.Overlaps(m12To16));

            //across days
            Assert.IsTrue(m12ToT12.Overlaps(m8To14));
            Assert.IsTrue(m8To14.Overlaps(m12ToT12));

            //across days and weeks
            Assert.IsTrue(s20ToM12.Overlaps(m8To14));
            Assert.IsTrue(m8To14.Overlaps(s20ToM12));
        }

        [Test]
        public void OverlapsReturnsFalseForNonOverlappingIntradayPeriods()
        {
            //todo write tests
        }
    }
}
