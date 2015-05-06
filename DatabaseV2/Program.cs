using System;

namespace DatabaseV2
{
    /// <summary>
    /// The entry point of the program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The entry point of the program.
        /// </summary>
        /// <param name="args">The arguments to the program.</param>
        private static void Main(string[] args)
        {
            DatabaseNode node = new DatabaseNode(Settings.ReadCommandLineArguments(args));
            while (Console.ReadLine() != "exit")
            {
            }

            Console.WriteLine("Shutting down...");
            node.Shutdown();
        }
    }
}