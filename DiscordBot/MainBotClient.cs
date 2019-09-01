using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace DiscordBot
{
    public class MainBotClient
    {
        private DiscordSocketClient _Client;
        private CommandService _Commands;
        private IServiceProvider _Services;

        public MainBotClient(DiscordSocketClient client = null, CommandService cmdService = null)
        {
            _Client = client ?? new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Debug
            });

            _Commands = cmdService ?? new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Verbose
            });
        }
        public async Task InitializeAsync()
        {
            string path = Path.GetFullPath(@"Data") + "\\Token.txt";
            string token = "";
            using (StreamReader sr = new StreamReader(@"Data\\Token.txt"))
            {
                string line = sr.ReadLine();
                token = line;
                sr.Close();
            }

            await _Client.LoginAsync(TokenType.Bot, token);
            await _Client.StartAsync();
            _Client.Log += LogAsync;
            _Services = SetupServices();
            var cmdHandler = new CommandHandler(_Client, _Commands, _Services);
            await cmdHandler.InitializeAsync();
            await _Services.GetRequiredService<MusicService>().InitializeAsync();
            await Task.Delay(-1);
        }
        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }

        private IServiceProvider SetupServices()
           => new ServiceCollection()
           .AddSingleton(_Client)
           .AddSingleton(_Commands)
           .AddSingleton<LavaRestClient>()
           .AddSingleton<LavaSocketClient>()
           .AddSingleton<MusicService>()
           .BuildServiceProvider();
    }
}
