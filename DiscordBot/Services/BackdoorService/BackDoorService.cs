using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class BackDoorService
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<ulong, int> GuildLimit = new Dictionary<ulong, int>();
        private readonly Timer UpdateLinmit = new Timer
        {
            Interval = 5000
        };

        public bool CheckMessage(SocketUserMessage msg)
        {
            var chnl = msg.Channel as SocketGuildChannel;
            var guildId = chnl.Guild.Id;
            if (!GuildLimit.ContainsKey(guildId)) GuildLimit.Add(guildId, 0);
            if (GuildLimit[guildId] >= 5) return false;
            GuildLimit[guildId]++;
            return true;
        }
        private void Updated(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                foreach (var Key in GuildLimit.Keys)
                {
                    if (GuildLimit[Key] <= 0) GuildLimit.Remove(Key);
                    else GuildLimit[Key]--;
                }
            }
            catch (Exception)
            { }
        }
        public Task InitializeAsync()
        {
            UpdateLinmit.Elapsed += Updated;
            UpdateLinmit.Start();
            return Task.CompletedTask;
        }
        public BackDoorService(DiscordSocketClient client)
        {
            _client = client;
        }
    }
}
