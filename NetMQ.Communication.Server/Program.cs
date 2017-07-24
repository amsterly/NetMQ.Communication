using NetMQ.Communication.Server;
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
            Console.WriteLine("请输入以下数字进入对应功能模块：");
            Console.WriteLine("1.ReqRepPubSub混合模式");
            Console.WriteLine("2.SubPuc模式");
            int i = Convert.ToInt32(Console.ReadLine());
            switch (i)
            {
                case 1: mixMode(); break;
                case 2: subPubMode(); break;
                case 3: Environment.Exit(0); break;
            }
        }

        private static void subPubMode()
        {
            AlyServer_Pub_BeaconVersion service = new AlyServer_Pub_BeaconVersion("AlyServer");
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

        private static void mixMode()
        {
            AlyServer_RepPubVersion server = new AlyServer_RepPubVersion();

            server.start();
            do
            {
                Console.WriteLine("Please enter what u want to send to the client before press the enter button.");
                string msg = Console.ReadLine();
                if (string.IsNullOrEmpty(msg)) break;
                server.sendMsg(msg);
            } while (true);
            server.stop();

        }
    }
}
