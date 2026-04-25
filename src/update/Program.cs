using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using update.Parameters;

namespace update
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Try parsing parameters
            var parameters = await GetObjectOrDefaultAsync(() => Task.FromResult(UpdateParameters.Parse(args))!);
            if (parameters is null)
                return;

            // Close current application that needs an update
            if (!WaitForClose(parameters.InitialExecutable, 10))
            {
                Console.WriteLine($"Could not close '{parameters.InitialExecutable}'. Exiting...");
                return;
            }

            // Get update files
            var updater = new Updater(parameters);
            var updateFile = await GetObjectOrDefaultAsync(() => updater.GetUpdateFile()!);
            if (updateFile is null)
                return;

            var manifest = await GetObjectOrDefaultAsync(() => updater.GetManifest());
            if (manifest is null)
                return;

            // Extract the update files to the application directory
            using var zipArchive = new ZipArchive(updateFile, ZipArchiveMode.Read);

            var currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            Console.Write($"Extract update to directory '{currentDirectory}'... ");
            zipArchive.ExtractToDirectory(currentDirectory, true);
            Console.WriteLine("OK");

            // Start the updated application
            Console.Write($"Start updated application '{parameters.InitialExecutable}'... ");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(manifest.ApplicationName)
            };
            process.Start();

            Console.WriteLine("OK");
        }

        /// <summary>
        /// Executes any given <see cref="Func{TObject}"/> and catches all exceptions.
        /// </summary>
        /// <typeparam name="TObject">The return type of the delegate.</typeparam>
        /// <param name="getFunc">The delegate to execute.</param>
        /// <returns>The asynchronous operation returning the object.</returns>
        private static async Task<TObject?> GetObjectOrDefaultAsync<TObject>(Func<Task<TObject?>> getFunc) where TObject : class
        {
            try
            {
                return await getFunc();
            }
            catch (InvalidOperationException ioe)
            {
                Console.WriteLine(ioe.Message);
                return null;
            }
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
