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
            Network network = new Network(int.Parse(args[0]));
            network.Connect(new NodeDefinition("localhost", int.Parse(args[1])));

            while (true)
            {
                Thread.Sleep(1000);
                Console.Clear();
                Console.WriteLine("Connected Nodes:");
                foreach (var node in network.GetConnectedNodes())
                {
                    Console.WriteLine(node.ConnectionName);
                }
            }
        }
    }
}