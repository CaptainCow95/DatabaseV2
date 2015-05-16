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
            var settings = Settings.ReadCommandLineArguments(args);

            if (settings.Controller)
            {
                new ControllerNode(settings).Run();
            }
            else
            {
                new DatabaseNode(settings).Run();
            }
        }
    }
}