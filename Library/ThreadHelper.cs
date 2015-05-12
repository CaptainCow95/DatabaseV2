using System;
using System.Threading;

namespace Library
{
    public static class ThreadHelper
    {
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