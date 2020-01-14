using System;
using System.Collections.Generic;
using System.Text;
using Victoria;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Services.Common
{
    public class LavaTrackAdapter : LavaTrack
    {
        public SocketGuildUser Provider { get; set; }
    }
}
