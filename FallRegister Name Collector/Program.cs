using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace FallRegister_Name_Collector
{
    class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);


        static Options LoadOptions()
        {
            string optionsFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FallRegisterOptions.json";
            if (File.Exists(optionsFileName))
            {
                string json = "";
                StreamReader file = new StreamReader(optionsFileName);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    json += line;
                }
                file.Close();

                byte[] data = Encoding.UTF8.GetBytes(json);
                MemoryStream ms = new MemoryStream(data);

                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Options));
                Options o = ser.ReadObject(ms) as Options;

                return o;
            }
            else
            {
                Options o = new Options();
                o.userToken = "";
                o.hideConsole = false;
                return o;
            }
        }

        static void SaveOptions(Options opt)
        { 
            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Options));
            ser.WriteObject(ms, opt);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string jsonString = sr.ReadToEnd();

            string optionsFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FallRegisterOptions.json";
            if (File.Exists(optionsFileName))
            {
                File.Delete(optionsFileName);
            }
            using StreamWriter file = new StreamWriter(optionsFileName, true);
            file.WriteLine(jsonString);
        }

        static List<string> GetSavedUsernames()
        {
            string checkedUsersFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FallGuysNames.log";
            if (File.Exists(checkedUsersFileName))
            {
                List<string> knownNames = new List<string>();
                StreamReader file = new StreamReader(checkedUsersFileName);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    knownNames.Add(line);
                }
                file.Close();
                return knownNames;
            }
            return new List<string>();
        }

        static void SaveUsernameLocally(string name)
        {
            string usersFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FallGuysNames.log";
            using StreamWriter file = new StreamWriter(usersFileName, true);
            file.WriteLine(name);
        }

        static bool Authorize(string token)
        {
            WebRequest request = WebRequest.Create("https://fallregister.com/api/?authenticate&key=" + token);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            if (responseFromServer.Contains("\"err_id\""))
            {
                return false;
            }
            else
            {
                Console.WriteLine("Logged in as {0}\n", responseFromServer);
                return true;
            }
        }
        static void Main(string[] args)
        {
            Options opt = LoadOptions();
            if (opt.setupDone)
            {
                if (Authorize(opt.userToken))
                {
                    if (opt.hideConsole)
                    {
                        IntPtr hWnd = GetConsoleWindow();
                        if (hWnd != IntPtr.Zero)
                        {
                            ShowWindow(hWnd, 0);
                        }
                    }
                    Console.WriteLine("Running and Listening!");
                    List<string> knownNames = GetSavedUsernames();
                    while (true)
                    {
                        string line;
                        string ownLog = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Player.log";
                        string logPath = @"C:/Users/LeBogo/AppData/LocalLow/Mediatonic/FallGuys_client/Player.log";

                        if (File.Exists(logPath))
                        {
                            if (File.Exists(ownLog))
                            {
                                File.Delete(ownLog);
                            }
                            File.Copy(logPath, ownLog);

                            StreamReader file = new StreamReader(ownLog);
                            while ((line = file.ReadLine()) != null)
                            {
                                if (line.Contains("[StateGameLoading] OnPlayerSpawned"))
                                {
                                    string pattern = @"\d{2}:\d{2}:\d{2}.\d{3}: \[StateGameLoading\] OnPlayerSpawned - name=FallGuy \[\d{1,1000}\] ";
                                    line = System.Text.RegularExpressions.Regex.Replace(line, pattern, "");
                                    pattern = @" ID=\d{1,1000} was spawned";
                                    string name = System.Text.RegularExpressions.Regex.Replace(line, pattern, "");

                                    if (!knownNames.Contains(name))
                                    {
                                        knownNames.Add(name);
                                        SaveUsernameLocally(name);
                                        WebRequest request = WebRequest.Create("https://fallregister.com/api/?key=" + opt.userToken + "&insert=" + name);
                                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                        Stream dataStream = response.GetResponseStream();
                                        StreamReader reader = new StreamReader(dataStream);
                                        string responseFromServer = reader.ReadToEnd();
                                        if (!responseFromServer.Contains("error"))
                                        {
                                            Console.WriteLine("[I] Inserted {0} to FallRegister database.", name);
                                        }
                                        else
                                        {
                                            Console.WriteLine("[E] {0} already exists in database.", name);

                                        }

                                        reader.Close();
                                        dataStream.Close();
                                        response.Close();
                                    }
                                }
                            }
                            file.Close();
                        }
                        Thread.Sleep(10000);

                    }
                } else
                {
                    Console.WriteLine("Your token is invalid.");
                    Console.ReadLine();
                }
            }
            else
            {
                bool tokenValid = false;
                string token = "";
                while (!tokenValid)
                {
                    Console.Write("Please enter your FallRegister token: ");
                    token = Console.ReadLine();
                    tokenValid = Authorize(token);
                    if (!tokenValid)
                    {
                        Console.WriteLine("Token Invalid. Please try again.");
                    }
                }
                Console.WriteLine("");
                Console.Write("Hide Console on startup? (Y/n) (Default: Y): ");
                string hideOnStartup = Console.ReadLine();
                bool hideConsole = true;
                if (hideOnStartup.ToLower().Equals("n")) {
                    hideConsole = false;
                }

                Options newOptions = new Options();
                newOptions.hideConsole = hideConsole;
                newOptions.userToken = token;
                newOptions.setupDone = true;
                SaveOptions(newOptions);
                Console.WriteLine("");
                Console.WriteLine("Alright! Setup done!");
                Console.WriteLine("If you want to launch this program every time you start your Computer:");
                Console.WriteLine("Press 'Windows Key' + 'R'");
                Console.WriteLine("Type in 'Autostart'");
                Console.WriteLine("Copy this program into the Autostart folder.");
                Console.WriteLine("");
                Console.WriteLine("If you wish to redo the Setup simply delete 'FallRegisterOptions.json' in your Documents folder.");
                if (newOptions.hideConsole)
                {
                    Console.WriteLine("\n\nThis window will hide in 30 seconds.");
                    Thread.Sleep(15000);
                }
                
                Main(args);

            }
        }
    }
}
