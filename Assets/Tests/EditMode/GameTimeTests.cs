using NUnit.Framework;
using WalkingIntoNight.TRPG.Core;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class GameTimeTests
    {
        [Test]
        public void AdvancePeriod_TraversesPeriods_AndOnlyIncrementsDayAfterNight()
        {
            var time = GameTime.Default;

            time.AdvancePeriod();
            Assert.That(time.period, Is.EqualTo(TimePeriod.Afternoon));
            Assert.That(time.day, Is.EqualTo(1));

            time.AdvancePeriod();
            Assert.That(time.period, Is.EqualTo(TimePeriod.Evening));
            Assert.That(time.day, Is.EqualTo(1));

            time.AdvancePeriod();
            Assert.That(time.period, Is.EqualTo(TimePeriod.Night));
            Assert.That(time.day, Is.EqualTo(1));

            time.AdvancePeriod();
            Assert.That(time.period, Is.EqualTo(TimePeriod.Morning));
            Assert.That(time.day, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-period")]
        public void ParsePeriod_WhenValueIsEmptyOrUnknown_FallsBackToMorning(string value)
        {
            Assert.That(GameTime.ParsePeriod(value), Is.EqualTo(TimePeriod.Morning));
        }
    }
}
