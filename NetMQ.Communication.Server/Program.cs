using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ_Communication.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            RunConsole();
        }

        private static void RunConsole()
        {
            Console.Title = "Aly NetMQ Service";

            AlyServer_Pub service = new AlyServer_Pub("AlyServer");
            service.Start();

            do
            {
                Console.WriteLine("Please enter what u want to send to the client before press the enter button.");
                string msg = Console.ReadLine();
                if (string.IsNullOrEmpty(msg)) break;
                service.PubCCR(msg);
            } while (true);

            service.Stop();
        }
    }
}
