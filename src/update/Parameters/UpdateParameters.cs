using System;
using System.Collections.Generic;

namespace update.Parameters
{
    /// <summary>
    /// The class holding the input parameters for an update operation.
    /// </summary>
    class UpdateParameters
    {
        private static readonly IDictionary<Application, string> BaseUrls = new Dictionary<Application, string>
        {
            [Application.CommandLineWindows] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-CommandLine-Update/main/Windows",
            [Application.CommandLineLinux] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-CommandLine-Update/main/Linux",
            [Application.CommandLineMac] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-CommandLine-Update/main/Mac",
            [Application.EtoFormsWpf] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-EtoForms-Update/main/Wpf",
            [Application.EtoFormsGtk] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-EtoForms-Update/main/Gtk",
            [Application.EtoFormsMac] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-EtoForms-Update/main/Mac"
        };

        /// <summary>
        /// The application to request the update of.
        /// </summary>
        public Application Application { get; }

        /// <summary>
        /// The path to the executable, that was running before the update process.
        /// </summary>
        public string InitialExecutable { get; }

        /// <summary>
        /// The base URL to the update resources.
        /// </summary>
        public string BaseUrl { get; }

        private UpdateParameters(Application application, string initialExecutable)
        {
            Application = application;
            InitialExecutable = initialExecutable;
            BaseUrl = BaseUrls[application];
        }

        /// <summary>
        /// Parse command line arguments to create an instance of <see cref="UpdateParameters"/>.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>An instance of <see cref="UpdateParameters"/>.</returns>
        public static UpdateParameters Parse(string[] args)
        {
            if (args.Length < 2)
                throw new InvalidOperationException("At least 2 arguments have to be given.");

            if (!Enum.TryParse<Application>(args[0].Replace(".", ""), out var application))
                throw new InvalidOperationException($"Invalid application monicker '{args[0]}'.");

            if (!BaseUrls.ContainsKey(application))
                throw new InvalidOperationException($"Application {args[0]} is not supported.");

            return new UpdateParameters(application, args[1]);
        }
    }
}
