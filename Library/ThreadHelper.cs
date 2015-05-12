using System;
using System.Threading;

namespace Library
{
    /// <summary>
    /// Some helper functions for threads.
    /// </summary>
    public static class ThreadHelper
    {
        /// <summary>
        /// Sleeps for a specified amount of time in 250 millisecond increments, returning after the specified time or running evaluates to false, whichever comes first.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds to sleep for.</param>
        /// <param name="running">A function to determine whether the thread is still running.</param>
        public static void ResponsiveSleep(int milliseconds, Func<bool> running)
        {
            while (milliseconds > 0 && running())
            {
                int timestep = Math.Min(milliseconds, 250);
                milliseconds -= timestep;
                Thread.Sleep(timestep);
            }
        }
    }
}