using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CerbiClientLogging.Classes.Kafka
{
    internal class StreamSet
    {
        internal void kafkaBase()
        {
            // initial settings for cosmos kafka stream
            var msg = "";
            var path = @"C:\Program Files\Confluent-5.0.0\bin\";

            var localHost = ConfigurationManager.AppSettings["KAFKA-BROKER"];
            var consumerTopic = $"{ConfigurationManager.AppSettings["KAFKA-MEOW"]} >> C:\\tmp\\meow-kafka-consumer.txt";
            var producerTopic = " >> C:\\tmp\\meow-kafka-producer.txt";
            var myTopic = ConfigurationManager.AppSettings["KAFKA-MEOW"];
            var myMessage = ConfigurationManager.AppSettings["KAFKA-MESSAGE"];
            var externalFile = @"C:\Users\pthakrar\source\repos\Cerbi-logging\Cerbi-logging\TestLogs\LOGS.json";
            var jsonLinesFile = File.ReadLines(externalFile);
            var testFile = @"C:\tmp\meow-kafka-producer.txt";

            string[] jsonInput = jsonLinesFile.ToArray();
            for (var i = 0; i < 4; i++)
            {
                msg += jsonInput[i];
                Debug.WriteLine(msg);
            }

            msg += ",";
            Debug.WriteLine(msg);
            string kafkaCommand = $"echo '{msg}' | kafka-console-producer --topic {myTopic} --broker-list {localHost}";

            Console.WriteLine(msg);

            //System.Diagnostics.Process process = new System.Diagnostics.Process();

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = Path.Combine(path, "kafka-console-producer.bat");
                    process.StartInfo.WorkingDirectory = path;
                    process.StartInfo.Arguments = kafkaCommand;
                    process.StartInfo.CreateNoWindow = false;
                    process.Start();
                    //process.WaitForExit();
                }
            }
            catch
            {
                //TODO: an error occurred writing to the stream, notify using central alarm functionality (zabbix?)
            }


            // start below was just a test
            /*using (var process = Process.Start("CMD.exe", "/C kafka-console-producer --help | more"))
            {
                process.WaitForExit();
                //if (process.ExitCode == 0)
                //{
                //    Console.WriteLine("Everything went OK");
                //}
            }*/
            // end of test run

            //var p = new Process();
            //Process.Start("meowtestfile-kafka-consumer.txt", "");

            //var cybertruck = new Process.Start("echo 'HAL' | kafka-console-producer --topic meowtopic --broker-list localhost:9092"));
            //var executedMsg = series9.Start();


            /**************** master brach made local changes after ****************/

            /****************** -- END -- *****************************************/


            //ProcessStartInfo cosmosProcess = new ProcessStartInfo(cosmosRun);
            //var cosmosRun2 = ConfigurationManager.AppSettings["KAFKA-CONSUMER"] + @"echo " + "\"" + myMessage + "\"" + @" | kafka-console-producer --topic meowtopic --broker list localhost:9092";
            //ExecuteProcess(kafkaCommand);
            //ExecuteProcess(cosmosRun2);
        }

        public void ExecuteProcess(string processCommand)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processCommand
                }
            };
            process.Start();
        }
    }
}


//var process = new ProcessStartInfo
//{
//    FileName = @"C:\Program Files\Confluent-5.0.0\bin\kafka-console-producer.bat",
//    WorkingDirectory = @"C:\Program Files\Confluent-5.0.0\bin\",
//    //RedirectStandardInput = true,
//    //RedirectStandardOutput = true,
//    //UseShellExecute = false,
//    //CreateNoWindow = true
//};
//    System.Diagnostics.Process.Start(process);
//}