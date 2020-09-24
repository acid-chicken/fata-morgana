#nullable enable

using System;
using System.Threading.Tasks;

namespace AcidChicken.FataMorgana
{
    partial class Program
    {
#pragma warning disable IDE0060
        static async Task Main(string[] args /* IDE0060 */)
        {
#pragma warning restore IDE0060
            Console.WriteLine("============ Fata Morgana v2.0.0 ============");
            Console.WriteLine("Licensed for im@ssc oto-mad creators and etc.");
            Console.WriteLine("=============================================");
            Console.Write("% Launching server ...");

            ServerStart();

            Console.WriteLine(" Done.");
            Console.Write("% Launching watcher ...");

            WatcherStart();

            Console.WriteLine(" Done.");
            Console.WriteLine("% Ready for use.");
            Console.Write("% Checking for updates ...");

            if (await RequestArtifactUrl() is Uri uri)
            {
                Console.WriteLine(" Done.");
                Console.WriteLine($"% The new version of fata-morgana is available. Open below URL to download (it'll expire a minute later).");

                Console.ForegroundColor = ConsoleColor.Blue;

                Console.WriteLine(uri);

                Console.ResetColor();

                Console.Write("# Enter the license key (e.g. 12345-67891-01112): ");

                if (AuthorizeKey(Console.ReadLine()) is string key)
                {
                    Console.Write("% Authorization success. The password of the zip file is: ");

                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(key);

                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("% Authorization failed. Check the license key and relaunch fata-morgana to retry.");
                }
            }
            else
            {
                Console.WriteLine(" Done.");
                Console.WriteLine("% This version of fata-morgana is up to date (or newer version is unavailable for now).");
            }

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine();
                Console.Write("% Closing watcher ...");

                WatcherStop();

                Console.WriteLine(" Done.");
                Console.Write("% Closing server ...");

                ServerStop();

                Console.WriteLine(" Done.");
            };

            await Task.Delay(-1);
        }
    }
}
