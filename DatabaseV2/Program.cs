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
            Settings settings = new Settings();

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "--port")
                {
                    if (i + 1 < args.Length)
                    {
                        int port;
                        if (int.TryParse(args[i + 1], out port))
                        {
                            ++i;
                            if (port >= 1 && port <= 65535)
                            {
                                settings.Port = port;
                            }
                            else
                            {
                                Console.WriteLine("--port argument provided, but was not followed by a valid port between 1 and 65535.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("--port argument provided, but was not followed by a number.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("--port argument provided, but was not followed by a number.");
                    }
                }
                else if (arg == "--nodes")
                {
                    if (i + 1 < args.Length)
                    {
                        ++i;
                        settings.ParseNodeList(args[i + 1]);
                    }
                    else
                    {
                        Console.WriteLine("--nodes argument provided, but was not followed by a value.");
                    }
                }
            }

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