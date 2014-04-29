using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            //todo write tests
            
            //across days?
        }

        [Test]
        public void OverlapsReturnsFalseForNonOverlappingIntradayPeriods()
        {
            //todo write tests
        }
    }
}
