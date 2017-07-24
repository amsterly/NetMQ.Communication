using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace NetMQ.Communication.Client
{
    public class Options
    {
        public int HelloInterval { get; set; }

        public string RootPath { get; set; }

        public string ServerAddress { get; set; }

        public string MsgPubPort { get; set; }

        public string FilePubPort { get; set; }

        public string MsgResPort { get; set; }

        public string StationID { get; set; }

        private static string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static Options LoadFromAppSettings()
        {
            return new Options
            {
                HelloInterval = 10000,
                //RootPath = ConfigurationManager.AppSettings.AllKeys.Contains("RootPath") ? ConfigurationManager.AppSettings["RootPath"] : System.IO.Path.Combine(ApplicationData, "RMPeripheral"),
            };
        }
    }
}