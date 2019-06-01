using System;
using System.Threading.Tasks;

namespace AcidChicken.FataMorgana
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("大石泉");

            ServerStart();

            Console.Write("す");

            WatcherStart();

            Console.WriteLine("き");

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.Write("き");

                WatcherStop();

                Console.Write("す");

                ServerStop();

                Console.WriteLine("泉石大");
            };

            await Task.Delay(-1);
        }
    }
}
