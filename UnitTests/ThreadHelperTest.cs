using Library;
using NUnit.Framework;
using System;
using System.Threading;

namespace UnitTests
{
    /// <summary>
    /// Tests the <see cref="ThreadHelper"/> class.
    /// </summary>
    [TestFixture]
    public class ThreadHelperTest
    {
        /// <summary>
        /// Tests waiting for the entire sleep period.
        /// </summary>
        [Test]
        public void SleepFull()
        {
            DateTime start = DateTime.UtcNow;
            ThreadHelper.ResponsiveSleep(1000, () => true);
            DateTime end = DateTime.UtcNow;
            Assert.Greater((end - start).TotalMilliseconds, 900, "ResponsiveSleep exited earlier than was expected.");
        }

        /// <summary>
        /// Tests exiting the ResponseSleep function early.
        /// </summary>
        [Test]
        public void SleepPartial()
        {
            DateTime start = DateTime.UtcNow;
            bool running = true;
            Thread thread = new Thread(() =>
            {
                Thread.Sleep(100);
                running = false;
            });
            thread.Start();
            ThreadHelper.ResponsiveSleep(5000, () => running);
            DateTime end = DateTime.UtcNow;
            Assert.Less((end - start).TotalMilliseconds, 1500, "ResponsiveSleep exited later than was expected.");
        }
    }
}