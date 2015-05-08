using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Library.Logging
{
    /// <summary>
    /// A object to log messages to a file and the console.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// A queue of messages to be logged.
        /// </summary>
        private static readonly ConcurrentQueue<Tuple<string, LogLevel, DateTime>> Messages = new ConcurrentQueue<Tuple<string, LogLevel, DateTime>>();

        /// <summary>
        /// The minimum log level to log.
        /// </summary>
        private static LogLevel _logLevel;

        /// <summary>
        /// The location to log to.
        /// </summary>
        private static string _logLocation;

        /// <summary>
        /// The thread to run the logger.
        /// </summary>
        private static Thread _logThread;

        /// <summary>
        /// A value indicating whether the logger is running.
        /// </summary>
        private static bool _running = true;

        /// <summary>
        /// Initializes the logger.
        /// </summary>
        /// <param name="logLocation">The location to log to.</param>
        /// <param name="logLevel">The log level of the message.</param>
        public static void Init(string logLocation, LogLevel logLevel)
        {
            _logLocation = logLocation;
            _logLevel = logLevel;

            _logThread = new Thread(RunLogger);
            _logThread.Start();
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logLevel">The log level of the message.</param>
        public static void Log(string message, LogLevel logLevel)
        {
            Messages.Enqueue(new Tuple<string, LogLevel, DateTime>(message, logLevel, DateTime.UtcNow));
        }

        /// <summary>
        /// Shuts down the logger.
        /// </summary>
        public static void Shutdown()
        {
            _running = false;
        }

        /// <summary>
        /// Flush all available messages to the file and console.
        /// </summary>
        private static void FlushMessages()
        {
            StringBuilder text = new StringBuilder();
            while (!Messages.IsEmpty)
            {
                Tuple<string, LogLevel, DateTime> item;
                if (Messages.TryDequeue(out item) && item.Item2 <= _logLevel)
                {
                    text.AppendFormat("[{0} {1} {2}] {3}\n", item.Item3.ToShortDateString(), item.Item3.ToLongTimeString(), Enum.GetName(typeof(LogLevel), item.Item2), item.Item1);
                }
            }

            File.AppendAllText(Path.Combine(_logLocation, "debug.log"), text.ToString());
            Console.Write(text.ToString());
        }

        /// <summary>
        /// Run the logger.
        /// </summary>
        private static void RunLogger()
        {
            while (_running)
            {
                FlushMessages();

                Thread.Sleep(1000);
            }

            FlushMessages();
        }
    }
}