using Library;
using NUnit.Framework;
using System;
using System.Threading;

namespace UnitTests
{
    /// <summary>
    /// Tests the <see cref="ThreadHelper"/> class.
    /// </summary>
    [TestFixture(Category = "ThreadHelper")]
    public class ThreadHelperTest
    {
        /// <summary>
        /// Tests waiting for the entire sleep period.
        /// </summary>
        [Test(Description = "SleepFull")]
        public void TestSleepFull()
        {
            DateTime start = DateTime.UtcNow;
            ThreadHelper.ResponsiveSleep(2000, () => true);
            DateTime end = DateTime.UtcNow;
            Assert.Greater((end - start).TotalMilliseconds, 2000, "ResponsiveSleep exited earlier than was expected.");
        }

        /// <summary>
        /// Tests exiting the ResponseSleep function early.
        /// </summary>
        [Test(Description = "SleepPartial")]
        public void TestSleepPartial()
        {
            DateTime start = DateTime.UtcNow;
            bool running = true;
            Thread thread = new Thread(() =>
            {
                Thread.Sleep(500);
                running = false;
            });
            thread.Start();
            ThreadHelper.ResponsiveSleep(5000, () => running);
            DateTime end = DateTime.UtcNow;
            Assert.Less((end - start).TotalMilliseconds, 1000, "ResponsiveSleep exited later than was expected.");
        }
    }
}