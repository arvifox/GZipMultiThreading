using System;

namespace zipthread
{
    class Program
    {
        static void Main(string[] args)
        {
            // use this property or Console.CancelKeyPress event to handle Ctrl+c
            Console.TreatControlCAsInput = true;

            //GZipStream
            GZipManager gzip = new GZipManager(args);
            Console.WriteLine("GZipTest is working...");
            gzip.StartWork();

            // waiting for finish or ctrl+c
            Console.WriteLine("Press Ctrl+c to exit");
            ConsoleKeyInfo cki = new ConsoleKeyInfo();
            while (!gzip.isDone)
            {
                if (Console.KeyAvailable)
                {
                    cki = Console.ReadKey(true);
                    if ((cki.Key == ConsoleKey.C) && (cki.Modifiers & ConsoleModifiers.Control) != 0)
                    {
                        gzip.StopWork();
                        break;
                    }
                }
            }
            Console.WriteLine("GZipTest has done.");
            if (!gzip.resultOk)
            {
                Environment.Exit(1);
            }
        }
    }
}
