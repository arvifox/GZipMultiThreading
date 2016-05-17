using System;
using System.Diagnostics;

namespace zipthread
{
    class Program
    {
        static void Main(string[] args)
        {
            // use this property or Console.CancelKeyPress event to handle Ctrl+c
            Console.TreatControlCAsInput = true;

            // Stopwatch
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //GZipStream
            IGZipThread gzip = Factories.CreateGZipManager(args);
            Console.WriteLine("GZipTest is working...");
            gzip.StartThread();

            // waiting for finish or ctrl+c
            Console.WriteLine("Press Ctrl+c to exit");
            ConsoleKeyInfo cki = new ConsoleKeyInfo();
            while (!gzip.IsDone())
            {
                if (Console.KeyAvailable)
                {
                    cki = Console.ReadKey(true);
                    if ((cki.Key == ConsoleKey.C) && (cki.Modifiers & ConsoleModifiers.Control) != 0)
                    {
                        gzip.AbortThread();
                        gzip.JoinThread();
                        break;
                    }
                }
            }
            Console.WriteLine("GZipTest has done.");
            sw.Stop();
            Console.WriteLine("Time elapsed: {0}", sw.Elapsed);
            if (!gzip.ResultOK())
            {
                Console.WriteLine("Error code = 1");
                Environment.Exit(1);
            }
        }
    }
}
