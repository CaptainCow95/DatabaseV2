using DatabaseLibrary.Networking;
using System;
using System.Threading;

namespace DatabaseV2
{
    /// <summary>
    /// The entry point of the program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point of the program.
        /// </summary>
        /// <param name="args">The arguments to the program.</param>
        private static void Main(string[] args)
        {
            ChordNetwork network = new ChordNetwork(int.Parse(args[0]), new NodeDefinition("localhost", int.Parse(args[1])));

            while (true)
            {
                Thread.Sleep(1000);
                Console.Clear();
                Console.WriteLine("Running on port " + int.Parse(args[0]));
                Console.WriteLine("Printing Chord Status:");
                network.PrintStatus();
            }
        }
    }
}