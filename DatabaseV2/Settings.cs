using DatabaseV2.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DatabaseV2
{
    /// <summary>
    /// Represents settings for the database to use.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The help message.
        /// </summary>
        private const string HelpMessage =
@"--port <number>, -p <number>     Sets the port to the specified number.
--nodes <string>, -n <string>    Sets the initial nodes to connect to.
--enablewebinterface, -w         Enables the web interface.
--loglocation, -l                Sets the log location.
--help, -h                       Shows this help text.";

        /// <summary>
        /// The log level argument's error message.
        /// </summary>
        private const string LogLevelErrorMessage = " argument provided, but was not followed by a value.";

        /// <summary>
        /// The log level argument's invalid message.
        /// </summary>
        private const string LogLevelInvalidMessage = " argument provided, but was invalid. Valid options are 'debug', 'info', 'warning', and 'error'.";

        /// <summary>
        /// The log location argument's error message.
        /// </summary>
        private const string LogLocationErrorMessage = " argument provided, but was not followed by a value.";

        /// <summary>
        /// The nodes argument's error message.
        /// </summary>
        private const string NodesErrorMessage = " argument provided, but was not followed by a value.";

        /// <summary>
        /// The nodes argument's invalid message.
        /// </summary>
        private const string NodesInvalidMessage = "Connection string is not in the right format.";

        /// <summary>
        /// The port argument's conversion error message.
        /// </summary>
        private const string PortConversionErrorMessage = " argument provided, but was not followed by a number.";

        /// <summary>
        /// The port argument's error message.
        /// </summary>
        private const string PortErrorMessage = " argument provided, but was not followed by a number.";

        /// <summary>
        /// The port argument's invalid message.
        /// </summary>
        private const string PortInvalidMessage = " argument provided, but was not followed by a valid port between 1 and 65535.";

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="port">The port to run the database on.</param>
        /// <param name="nodes">The nodes to initially connect to.</param>
        /// <param name="enableWebInterface">A value indicating whether the web interface should be enabled.</param>
        /// <param name="logLocation">The location to log to.</param>
        /// <param name="logLevel">The minimum log level to log messages.</param>
        private Settings(int port, List<NodeDefinition> nodes, bool enableWebInterface, string logLocation, LogLevel logLevel)
        {
            Port = port;
            Nodes = nodes;
            EnableWebInterface = enableWebInterface;
            LogLocation = logLocation;
            LogLevel = logLevel;
        }

        /// <summary>
        /// Gets a value indicating whether the web interface should be enabled.
        /// </summary>
        public bool EnableWebInterface { get; private set; }

        /// <summary>
        /// Gets the minimum log level to log.
        /// </summary>
        public LogLevel LogLevel { get; private set; }

        /// <summary>
        /// Gets the location to log to.
        /// </summary>
        public string LogLocation { get; private set; }

        /// <summary>
        /// Gets the nodes to initially connect to.
        /// </summary>
        public List<NodeDefinition> Nodes { get; private set; }

        /// <summary>
        /// Gets the port to run the database on.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Reads the settings from command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments to read from.</param>
        /// <returns>The settings retrieved from the command line arguments.</returns>
        public static Settings ReadCommandLineArguments(string[] args)
        {
            Settings settings = new Settings(-1, new List<NodeDefinition>(), false, Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), LogLevel.Warning);
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = ReadArgument(args, i, string.Empty).ToLowerInvariant();
                switch (arg)
                {
                    case "--port":
                    case "-p":
                        int? port = ReadArgumentInt(args, ++i, arg + PortErrorMessage, arg + PortConversionErrorMessage);
                        if (port.HasValue)
                        {
                            if (port < 1 || port > 65535)
                            {
                                Logger.Log(arg + PortInvalidMessage, LogLevel.Warning);
                            }
                            else
                            {
                                settings.Port = port.Value;
                            }
                        }

                        break;

                    case "--nodes":
                    case "-n":
                        string nodeString = ReadArgument(args, ++i, arg + NodesErrorMessage);
                        try
                        {
                            settings.Nodes.AddRange(nodeString.Split(',').Select(e => new NodeDefinition(e)));
                        }
                        catch (ArgumentException)
                        {
                            settings.Nodes.Clear();
                            Logger.Log(NodesInvalidMessage, LogLevel.Warning);
                        }

                        break;

                    case "--enablewebinterface":
                    case "-w":
                        settings.EnableWebInterface = true;
                        break;

                    case "--loglocation":
                    case "-l":
                        string logLocation = ReadArgument(args, ++i, arg + LogLocationErrorMessage);
                        if (logLocation != string.Empty)
                        {
                            settings.LogLocation = logLocation;
                        }

                        break;

                    case "--loglevel":
                        string logLevel = ReadArgument(args, ++i, arg + LogLevelErrorMessage).ToLower();
                        if (logLevel != string.Empty)
                        {
                            if (logLevel == "debug")
                            {
                                settings.LogLevel = LogLevel.Debug;
                            }
                            else if (logLevel == "info")
                            {
                                settings.LogLevel = LogLevel.Info;
                            }
                            else if (logLevel == "warning")
                            {
                                settings.LogLevel = LogLevel.Warning;
                            }
                            else if (logLevel == "error")
                            {
                                settings.LogLevel = LogLevel.Error;
                            }
                            else
                            {
                                Logger.Log(arg + LogLevelInvalidMessage, LogLevel.Warning);
                            }
                        }

                        break;

                    case "--help":
                    case "-h":
                        Console.WriteLine(HelpMessage);
                        break;
                }
            }

            if (settings.Port == -1)
            {
                Logger.Log("No port given, defaulting to 5000", LogLevel.Warning);
                settings.Port = 5000;
            }

            if (settings.Nodes.Count == 0)
            {
                Logger.Log("No nodes specified to connect to.", LogLevel.Warning);
            }

            return settings;
        }

        /// <summary>
        /// Reads an argument from a command line list.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="index">The index of the argument to read.</param>
        /// <param name="errorMessage">The error message if the index is out of bounds.</param>
        /// <returns>The value of the argument at the specified index.</returns>
        private static string ReadArgument(string[] args, int index, string errorMessage)
        {
            if (index >= args.Length)
            {
                Logger.Log(errorMessage, LogLevel.Warning);
                return string.Empty;
            }

            return args[index];
        }

        /// <summary>
        /// Reads an argument from a command line list.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="index">The index of the argument to read.</param>
        /// <param name="errorMessage">The error message if the index is out of bounds.</param>
        /// <param name="conversionErrorMessage">The error message if the conversion to an integer fails.</param>
        /// <returns>The value of the argument at the specified index.</returns>
        private static int? ReadArgumentInt(string[] args, int index, string errorMessage, string conversionErrorMessage)
        {
            string arg = ReadArgument(args, index, errorMessage);
            if (arg == string.Empty)
            {
                return null;
            }

            int retValue;
            if (!int.TryParse(arg, out retValue))
            {
                Logger.Log(conversionErrorMessage, LogLevel.Warning);
            }

            return retValue;
        }
    }
}