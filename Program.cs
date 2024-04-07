using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        // Парсинг аргументов командной строки
        var arguments = ParseArguments(args);

        // Проверка наличия всех обязательных аргументов
        if (arguments.ContainsKey("file-log") && arguments.ContainsKey("file-output") &&
            arguments.ContainsKey("time-start") && arguments.ContainsKey("time-end"))
        {
            Logs(arguments);
        }
        else
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("--file-log <path> --file-output <path> --time-start <dd.MM.yyyy> --time-end <dd.MM.yyyy> [--address-start <address> --address-mask <mask>]");
        }
    }

    static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i += 2)
        {
            arguments.Add(args[i].TrimStart('-'), args[i + 1]);
        }

        return arguments;
    }
    static void Logs(Dictionary <string, string> arguments)
    {
        using (var file = new StreamReader(arguments["file-log"]))
        {
            var startTime = DateTime.ParseExact(arguments["time-start"], "dd.MM.yyyy", CultureInfo.InvariantCulture);
            var endTime = DateTime.ParseExact(arguments["time-end"], "dd.MM.yyyy", CultureInfo.InvariantCulture);
            var logs = new Dictionary<string, int>();
            string line;
            while((line = file.ReadLine()) != null) 
            {
                var parts = line.Split(":",2);
                if (parts.Length == 2)
                {
                    var address = parts[0];
                    var timestamp = DateTime.ParseExact(parts[1].Trim(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if(InTimeRange(timestamp, startTime, endTime))
                    {
                        if(arguments.ContainsKey("address-start") && arguments.ContainsKey("address-mask"))
                        {
                            int cidrNotation = arguments["address-mask"].Split('.').Sum(octet => Convert.ToString(Convert.ToInt32(octet), 2).Count(bit => bit == '1'));
                            var addressIp = IPAddress.Parse(address);
                            var network = new IPNetwork(IPAddress.Parse(arguments["address-start"]), cidrNotation);
                            if(network.Contains(addressIp))
                            {
                                if (logs.ContainsKey(address))
                                {
                                    logs[address]++;
                                }
                                else
                                    logs[address] = 1;
                            }
                        }
                        else
                        {
                            if (logs.ContainsKey(address))
                            {
                                logs[address]++;
                            }
                            else
                                logs[address] = 1;
                        }
                    }
                }
            }
            using (var output = new StreamWriter(arguments["file-output"]))
            {
                foreach(var log in logs)
                {
                    output.WriteLine($"{log.Key}: {log.Value}");
                }
            }
        }
    }

    static bool InTimeRange(DateTime timestamp, DateTime startTime, DateTime endTime)
    {
        return timestamp >= startTime && timestamp <= endTime;
    }
}
