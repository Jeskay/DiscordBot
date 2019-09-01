using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace DiscordBot
{
    class Program
    {
        static async Task Main(string[] args)
        => await new MainBotClient().InitializeAsync();
    }
}
