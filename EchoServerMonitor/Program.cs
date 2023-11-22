using System;
using System.Diagnostics;
using System.IO;

namespace EchoServerMonitor
{
    public class EchoProcess
    {
        public Process process;
        public string logsDir;

        public EchoProcess(string EchoPath, int timestep, string region)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = EchoPath,
                Arguments = $"-server -headless -noovr -nosymbollookup -timestep {timestep} -fixedtimestep -serverregion {region}",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            logsDir = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(EchoPath)!)!)!, "_local"),"r14logs");
        }
        public void Restart()
        {
            process.Kill();
            process.Start();
        }
        public bool IsRunning()
        {
            return !process.HasExited;
        }
        public void CheckErrors()
        {
            
            foreach(var file in Directory.GetFiles(logsDir))
            {
                if (file.Contains($"{process.Id}") && !file.Contains(".tmp"))
                {
                    File.Copy(file, file + ".tmp", true);
                    string[] lines = File.ReadAllLines(file + ".tmp");
                    string[] last50Strings = lines.Skip(Math.Max(0, lines.Length - 10)).ToArray();
                    foreach (var line in last50Strings)
                    {
                        string[] errors = { "Winsock Error 10060", "Lost connection (okay) to peer ws://", "[LOGIN] Log in request failed: Service unavailable", "[NETGAME] Failed to connect to the login service", "[TCP CLIENT] [R14NETCLIENT] Lost connection (okay) to peer ws://", "[TCP CLIENT] [R14NETCLIENT] Lost connection (error) to peer ws://" };
                        foreach (var error in errors)
                        {
                            if (line.Contains(error))
                            {
                                Console.WriteLine("Error found in server log, restarting server");
                                Restart();
                                return;
                            }
                        }
                        
                    }
                    File.Delete(file + ".tmp");
                }
            }
            
        }
    }



    
    class Program
    {
        static void Main(string[] args)
        {
            string? EchoPath = null;
            string? EchoRegion = null;
            int EchoCount = -1;
            int timestep = -1;
            
            if (args.Count() != 0)
            {
                if (File.Exists(args[0].Replace("\"", "")))
                {
                    EchoPath = args[0];
                }
            }
            if (args.Count() > 1)
            {
                try
                {
                    if (args[1] != null)
                    {
                        try
                        {
                            EchoCount = int.Parse(args[1]);
                        }
                        catch
                        {
                            Console.WriteLine("Server argument count must be a number");
                        }
                    }
                    if (args[2] != null)
                    {
                        try
                        {
                            timestep = int.Parse(args[2]);
                        }
                        catch
                        {
                            Console.WriteLine("timestep argument must be a number");
                        }
                    }
                    if (args[3] != null)
                    {
                        EchoRegion = args[3];
                    }
                }
                catch
                {

                }
            }
            if (EchoPath == null)
            {
                Console.WriteLine("Please enter the path to echovr.exe");
                while (true)
                {
                    EchoPath = Console.ReadLine()!.Replace("\"","");
                    if (File.Exists(EchoPath))
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("File not found, please enter the path to echovr.exe");
                    }
                }
            }
            EchoCount:
            if(EchoCount == -1 || EchoCount > 20)
            {
                if(EchoCount > 20)
                {
                    Console.WriteLine("Server count more than 20, that seems wrong.");
                }
                
                Console.WriteLine("Please enter the number of servers you want to run");
                while (true)
                {
                    try
                    {
                        EchoCount = int.Parse(Console.ReadLine()!);
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("Server count must be a number");
                    }
                }
                goto EchoCount;
            }

            if(timestep == -1)
            {
                Console.WriteLine("Please enter the timestep you want to use in form of tps, usually 120");
                while (true)
                {
                    try
                    {
                        timestep = int.Parse(Console.ReadLine()!);
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("timestep must be a number");
                    }
                }
            }
            
            if(EchoRegion == null)
            {
                Console.WriteLine("Please enter the region you want to use");
                EchoRegion = Console.ReadLine()!;

            }   

            EchoProcess[] processes = new EchoProcess[EchoCount];
            Console.WriteLine("Starting " + EchoCount + " servers");
            for (int i = 0; i < EchoCount; i++)
            {
                processes[i] = new EchoProcess(EchoPath,timestep,EchoRegion);
            }

            while (true)
            {
                for (int i = 0; i < EchoCount; i++)
                {
                    processes[i].CheckErrors();
                    if (!processes[i].IsRunning())
                    {
                        processes[i].Restart();
                        Console.WriteLine("Crash detected restarting server");
                    }
                }
                Thread.Sleep(5000);
            }
        }
    }

}
