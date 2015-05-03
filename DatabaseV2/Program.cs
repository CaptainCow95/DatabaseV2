using DatabaseV2.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

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
            int port = -1;
            List<NodeDefinition> nodes = new List<NodeDefinition>();
            bool enableWebInterface = false;
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "--port")
                {
                    if (i + 1 < args.Length)
                    {
                        if (int.TryParse(args[i + 1], out port))
                        {
                            ++i;
                            if (port < 1 && port > 65535)
                            {
                                port = -1;
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
                        var parts = args[i].Split(',');
                        try
                        {
                            nodes.AddRange(parts.Select(t => new NodeDefinition(t)));
                        }
                        catch (ArgumentException)
                        {
                            nodes.Clear();
                            Console.WriteLine("Connection string is not in the right format.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("--nodes argument provided, but was not followed by a value.");
                    }
                }
                else if (arg == "--enablewebinterface")
                {
                    enableWebInterface = true;
                }
            }

            if (port == -1)
            {
                Console.WriteLine("No port given, defaulting to 5000");
                port = 5000;
            }

            if (nodes.Count == 0)
            {
                Console.WriteLine("No nodes specified to connect to.");
            }

            DatabaseNode node = new DatabaseNode(new Settings(port, nodes, enableWebInterface));
            while (Console.ReadLine() != "exit")
            {
            }

            Console.WriteLine("Shutting down...");
            node.Shutdown();
        }
    }
}