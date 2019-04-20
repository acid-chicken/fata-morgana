using System;
using System.Threading.Tasks;

namespace AcidChicken.FataMorgana
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("大石泉");

            Start();

            Console.WriteLine("すき");

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.Write("きす");

                _server.Stop();

                Console.WriteLine("泉石大");
            };

            await Task.Delay(-1);
        }
    }
}
