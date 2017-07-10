﻿using Discord;
using Discord.WebSocket;

using IA.Addons;
using IA.Events;
using IA.FileHandling;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IA
{
    public class Bot
    {
        public AddonManager Addons { private set; get; }
        public DiscordShardedClient Client { private set; get; }
        public EventSystem Events { internal set; get; }

        public string Name
        {
            get
            {
                return clientInformation.Name;
            }
        }

        public string Version
        {
            get
            {
                return clientInformation.Version;
            }
        }

        public const string VersionNumber = "1.6";
        public const string VersionText = "IA v" + VersionNumber;

        public static Bot instance;

        private ClientInformation clientInformation;

        private string currentPath = Directory.GetCurrentDirectory();

        public Bot()
        {
            if (!File.Exists(currentPath + "/preferences.config"))
            {
                clientInformation = InitializePreferencesFile();
            }
            else
            {
                clientInformation = LoadPreferenceFile();
            }
            InitializeBot().GetAwaiter().GetResult();
        }
        public Bot(ClientInformation info)
        {
            clientInformation = info;
            InitializeBot().GetAwaiter().GetResult();
        }
        public Bot(Action<ClientInformation> info)
        {
            clientInformation = new ClientInformation();
            info.Invoke(clientInformation);
            InitializeBot().GetAwaiter().GetResult();
        }

        public void AddDeveloper(ulong id)
        {
            Events.Developers.Add(id);
        }
        public void AddDeveloper(IDiscordUser user)
        {
            Events.Developers.Add(user.Id);
        }
        public void AddDeveloper(IUser user)
        {
            Events.Developers.Add(user.Id);
        }

        public async Task ConnectAsync()
        {
            await Client.LoginAsync(TokenType.Bot, clientInformation.Token);

            foreach(DiscordSocketClient client in Client.Shards)
            {
                await client.StartAsync();
                // 10 seconds wait
                await Task.Delay(10000);
            }

            await Task.Delay(-1);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int GetShardId()
        {
            return clientInformation.ShardId;
        }

        public int GetTotalShards()
        {
            return clientInformation.ShardCount;
        }

        private ClientInformation InitializePreferencesFile()
        {
            ClientInformation outputBotInfo = new ClientInformation();
            FileWriter file = new FileWriter("preferences", "config");
            file.WriteComment(VersionText + " preferences file");
            file.WriteComment("Please do not change this file except to change\n# except to change your settings");
            file.WriteComment("Bot Name");
            Console.WriteLine("Enter bot name: ");
            string inputString = Console.ReadLine();
            file.Write(inputString);
            outputBotInfo.Name = inputString;

            file.WriteComment("Bot Token");
            Console.WriteLine("Enter bot token: ");
            inputString = Console.ReadLine();
            file.Write(inputString);
            outputBotInfo.Token = inputString;

            file.WriteComment("Shard count");
            Console.WriteLine("Shards [1-25565]:");
            inputString = Console.ReadLine();
            outputBotInfo.ShardCount = int.Parse(inputString);
            if (outputBotInfo.ShardCount < 1)
            {
                outputBotInfo.ShardCount = 1;
            }
            else if (outputBotInfo.ShardCount > 25565)
            {
                outputBotInfo.ShardCount = 25565;
            }

            file.Finish();

            return outputBotInfo;
        }

        private ClientInformation LoadPreferenceFile()
        {
            ClientInformation outputBotInfo = new ClientInformation();
            FileReader file = new FileReader("preferences", "config");
            outputBotInfo.Name = file.ReadLine();
            outputBotInfo.Token = file.ReadLine();
            file.Finish();
            return outputBotInfo;
        }

        private async Task InitializeBot()
        {
            instance = this;

            Log.InitializeLogging(clientInformation);

            Log.Message(VersionText);

            Client = new DiscordShardedClient(new DiscordSocketConfig()
            {
                TotalShards = clientInformation.ShardCount,
                LogLevel = LogSeverity.Info
            });

            EventSystem.RegisterBot(this);

            Events.RegisterPrefixInstance(">").RegisterAsDefault();
            // fallback prefix
            Events.RegisterPrefixInstance("miki.", false);
            // debug prefix
            Events.RegisterPrefixInstance("fmiki.", false, true);

            Addons = new AddonManager();
            await Addons.Load(this);

            if (clientInformation.EventLoaderMethod != null)
            {
                await clientInformation.EventLoaderMethod(this);
            }

            Application.ThreadException +=
               new ThreadExceptionEventHandler(Application_ThreadException);       

            foreach (DiscordSocketClient c in Client.Shards)
            {
                c.Ready += async () =>
                {
                    Log.Message($"shard {c.ShardId} ready!");
                    await c.SetGameAsync($"{c.ShardId}/{GetTotalShards()} | >help");
                };

                c.Connected += async () =>
                {
                    Log.Message($"{c.ShardId}| Connected!");
                };

                c.Disconnected += async (e) =>
                {
                    Log.ErrorAt(c.ShardId + "| Disconnected", e.Message);
                };
            }

            Client.Log += Client_Log;
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception.Message + "\n\n" + e.Exception.StackTrace);
        }

        private async Task Client_Log(LogMessage arg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(arg.Message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(arg.Exception);
            await Task.CompletedTask;
        }
    }
}