using System;
using System.Collections.Generic;

namespace update.Parameters
{
    class UpdateParameters
    {
        private static readonly IDictionary<Application, string> BaseUrls = new Dictionary<Application, string>
        {
            [Application.WinForms] = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-Update/master/Kuriimu2.WinForms"
        };

        public Application Application { get; }

        public string InitialExecutable { get; }

        public string BaseUrl { get; }

        private UpdateParameters(Application application, string initialExecutable)
        {
            Application = application;
            InitialExecutable = initialExecutable;
            BaseUrl = BaseUrls[application];
        }

        public static UpdateParameters Parse(string[] args)
        {
            if (args.Length < 2)
                throw new InvalidOperationException("At least 2 arguments have to be given.");

            if (!Enum.TryParse<Application>(args[0], out var application))
                throw new InvalidOperationException($"Invalid application indicator '{args[0]}'.");

            if (!BaseUrls.ContainsKey(application))
                throw new InvalidOperationException($"Application '{args[0]}' is not yet supported.");

            return new UpdateParameters(application, args[1]);
        }
    }
}
