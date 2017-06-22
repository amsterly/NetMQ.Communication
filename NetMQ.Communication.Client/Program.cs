using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Communication.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Aly Client";

            using (AlyClient_Subscriber client = new AlyClient_Subscriber("Aly Client "))
            {
                client.Start();
                client.CCRReady += client_CCRReady;
                Console.Read();

                client.Stop();
            }

            Console.ReadLine();
        }
        private static void client_CCRReady(object arg1, string arg2)
        {
            Console.WriteLine(arg2);
        }
    }
}
