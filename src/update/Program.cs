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
            if (!TryGetObject(() => UpdateParameters.Parse(args), out var parameters))
                return;

            if (!WaitForClose(parameters.InitialExecutable, 10))
            {
                Console.WriteLine($"Could not close '{parameters.InitialExecutable}'. Exiting...");
                return;
            }

            var updater = new Updater(parameters);
            if (!TryGetObject(() => updater.GetUpdateFile(), out var updateFile))
                return;
            if (!TryGetObject(() => updater.GetManifest(), out var manifest))
                return;

            using var zipArchive = new ZipArchive(updateFile, ZipArchiveMode.Read);

            var currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            Console.Write($"Extract update to directory '{currentDirectory}'... ");
            zipArchive.ExtractToDirectory(currentDirectory, true);
            Console.WriteLine("OK");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(manifest.ApplicationName)
            };
            process.Start();
        }

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
