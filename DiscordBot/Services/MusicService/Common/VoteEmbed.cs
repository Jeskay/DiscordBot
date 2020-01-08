﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace DiscordBot.Services.Common
{
    public class VoteEmbed
    {
        private EmbedBuilder embedBuilder;
        private SocketGuildUser _user;
        private LavaPlayer _player;
        public int Votes { get; set; }
        public IVoiceChannel channel { get; private set; }

        public async Task<Embed> Skip()
        {
            LavaTrack oldtrack = _player.Track;
            if (_player is null || _player.Queue.Count == 0)
            {
                if (_player.Track != null)
                {
                    await _player.SeekAsync(_player.Track.Duration);
                }
                embedBuilder.Description = $"пропущено {oldtrack.Title}";
                return embedBuilder.Build();
            }
            await _player.SkipAsync();
            embedBuilder.Description = $"пропущено {oldtrack.Title}, сейчас играет {_player.Track.Title}.";
            return embedBuilder.Build();
        }
        public Embed Voting()
        {
            embedBuilder.Description = $"{_user.Mention} начал голосование за пропуск песни **{_player.Track.Title}**. \n Нажмите на реакцию для пропуска трека.";
            return embedBuilder.Build();
        }
        public VoteEmbed(SocketGuildUser user, LavaPlayer player, SocketSelfUser selfuser)
        {
            embedBuilder = new EmbedBuilder();
            _user = user;
            _player = player;
            channel = player.VoiceChannel;
            embedBuilder.Footer = new EmbedFooterBuilder();
            embedBuilder.Footer.Text = "Для просмотра плейлиста | . q";
            embedBuilder.Footer.IconUrl = selfuser.GetAvatarUrl();
        }
    }
}
