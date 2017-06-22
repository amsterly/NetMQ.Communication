using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace NetMQ.Extension
{
    internal class Program
    {
        private static bool IsWinXP()
        {
            OperatingSystem OS = Environment.OSVersion;
            return OS.Version.Major <= 5;
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Any:{0}", IPAddress.Any);
            Console.WriteLine("Broadcast:{0}", IPAddress.Broadcast);
            Console.WriteLine("Loopback:{0}", IPAddress.Loopback);
            Console.WriteLine("None:{0}", IPAddress.None);

            if (IsWinXP())
            {
                AsyncIO.ForceDotNet.Force();
            }

            string name = args.Length > 0 ? args[1] : "Console";
            Beacon beacon = new Beacon("Beacon", name);
            beacon.NodeConnected += beacon_NodeConnected;
            beacon.NodeDisConnected += beacon_NodeDisConnected;
            beacon.Start();

            PrintAllNodes(beacon);

            string line;
            do
            {
                line = Console.ReadLine();
                if (line != null)
                    PrintAllNodes(beacon);
            } while (line != null);

            beacon.Stop();

            Console.ReadLine();
        }

        private static void beacon_NodeDisConnected(Beacon arg1, BeaconNode arg2)
        {
            PrintAllNodes(arg1);
        }

        private static void beacon_NodeConnected(Beacon arg1, BeaconNode arg2)
        {
            PrintAllNodes(arg1);
        }

        private static void PrintAllNodes(Beacon beacon)
        {
            Console.Clear();

            PrintTitle(beacon.SelfNode);

            Console.WriteLine("Linked Beacon Count: {0}", beacon.BeaconNodeCount);
            if (beacon.BeaconNodeCount == 0) return;

            int minLength = 8;
            string lineFormat1 = string.Format(@"{{0,-2}} {{1,-{0}}} {{2,-{1}}} {{3,-{2}}} {{4,-{3}}}",
                beacon.GetBeacons().Max(e => string.IsNullOrEmpty(e.Address) ? minLength : e.Address.Length),
                beacon.GetBeacons().Max(e => string.IsNullOrEmpty(e.Name) ? minLength : e.Name.Length),
                beacon.GetBeacons().Max(e => string.IsNullOrEmpty(e.Group) ? minLength : e.Group.Length),
                Math.Max(8, beacon.GetBeacons().Max(e => string.IsNullOrEmpty(e.HostName) ? minLength : e.HostName.Length)),
                beacon.GetBeacons().Max(e => string.IsNullOrEmpty(e.HostName) ? minLength : e.HostName.Length)
                );

            string lineFormat2 = string.Format(@"  {{0,-{0}}} {{1}}",
                Math.Max(8, beacon.GetBeacons().Max(e => string.IsNullOrEmpty(e.HostApp) ? minLength : e.HostApp.Length)));

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(lineFormat1, "No", "Address", "Name", "Group", "Host");
            Console.WriteLine(lineFormat2, "App", "Arguments");

            int index = 1;
            foreach (var each in beacon.GetBeacons())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(lineFormat1, index++, each.Address, each.Name, each.Group, each.HostName);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(lineFormat2, each.HostApp, each.Arguments);
            }
        }

        private static void PrintTitle(BeaconNode selfNode)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Beacon console is started. Press Ctrl+Z to exit.");
            Console.WriteLine("Current Beacon:");
            Console.WriteLine("{0,15}: {1}", "GUID", selfNode.Guid);
            Console.WriteLine("{0,15}: {1}", "Name", selfNode.Name);
            Console.WriteLine("{0,15}: {1}", "Group", selfNode.Group);
            Console.WriteLine("{0,15}: {1}", "Address", selfNode.Address);
            Console.WriteLine("{0,15}: {1}", "HostName", selfNode.HostName);
            Console.WriteLine("{0,15}: {1}", "Application", selfNode.HostApp);
            Console.WriteLine("{0,15}: {1}", "Arguments", selfNode.Arguments);
        }
    }
}