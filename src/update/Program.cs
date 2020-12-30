using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using update.Parameters;

namespace update
{
    class Program
    {
        static void Main(string[] args)
        {
            // Try parsing parameters
            if (!TryGetObject(() => UpdateParameters.Parse(args), out var parameters))
                return;

            // Close current application that needs an update
            if (!WaitForClose(parameters.InitialExecutable, 10))
            {
                Console.WriteLine($"Could not close '{parameters.InitialExecutable}'. Exiting...");
                return;
            }

            // Get update files
            var updater = new Updater(parameters);
            if (!TryGetObject(() => updater.GetUpdateFile(), out var updateFile))
                return;
            if (!TryGetObject(() => updater.GetManifest(), out var manifest))
                return;

            // Extract the update files to the application directory
            using var zipArchive = new ZipArchive(updateFile, ZipArchiveMode.Read);

            var currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            Console.Write($"Extract update to directory '{currentDirectory}'... ");
            zipArchive.ExtractToDirectory(currentDirectory, true);
            Console.WriteLine("OK");

            // Start the updated application
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(manifest.ApplicationName)
            };
            process.Start();
        }

        /// <summary>
        /// Executes any given <see cref="Func{TObject}"/> and catches all exceptions.
        /// </summary>
        /// <typeparam name="TObject">The return type of the delegate.</typeparam>
        /// <param name="getFunc">The delegate to execute.</param>
        /// <param name="result">The result of the delegate.</param>
        /// <returns>If the delegate was executed without throwing an exception.</returns>
        private static bool TryGetObject<TObject>(Func<TObject> getFunc, out TObject result) where TObject : class
        {
            result = null;

            try
            {
                result = getFunc();
            }
            catch (InvalidOperationException ioe)
            {
                Console.WriteLine(ioe.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Waits for a process to exit.
        /// </summary>
        /// <param name="process">The process to wait on.</param>
        /// <param name="timeOut">The timeout for the waiting cycle.</param>
        /// <returns>If the process exited successfully.</returns>
        private static bool WaitForClose(string process, int timeOut)
        {
            Console.Write($"Wait for '{process}' to close... ");

            var count = 0;
            while (count < timeOut)
            {
                if (!Process.GetProcessesByName(process).Any())
                {
                    Console.WriteLine("OK");
                    return true;
                }

                Thread.Sleep(1000);

                count++;
            }

            Console.WriteLine("FAIL");
            return false;
        }
    }
}
