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
            Console.Title = "AlyClient";

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
            using (AlyClient_Subscriber_BeaconVersion client = new AlyClient_Subscriber_BeaconVersion("AlyClient"))
            {
                client.Start();
                client.CCRReady += client_CCRReady;
                Console.Read();

                client.Stop();
            }

            Console.ReadLine();
        }
        private static void mixMode()
        {
            //请求一个用户输入的字符
            AlyClient_ReqSubVersion client = new AlyClient_ReqSubVersion();
            client.start();
            do
            {
                Console.WriteLine("Please enter what u want to send to the Server before press the enter button.");
                string msg = Console.ReadLine();
                if (string.IsNullOrEmpty(msg)) break;
                client.sendMSGtoServer(msg);
            } while (true);
            client.stop();

            

            
            
        }

        private static void client_CCRReady(object arg1, string arg2)
        {
            Console.WriteLine(arg2);
        }
    }
}
