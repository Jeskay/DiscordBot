﻿using System;
using System.Collections.Generic;
using System.Text;
using Victoria;
using Victoria.Entities;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace DiscordBot.Services.Common
{
    public class ControlPanel
    {
        private LavaPlayer _player;
        private EmbedBuilder _embed;
        private const int PositionChange = 10;
        private const int VolumeChange = 20;
        private const string botname = "Челик";
        private const string SiteURL = "https://docs.google.com";

        public SocketGuildUser Provider { get; private set; }
        public bool Repeat { get; set; }
        private string Writetime(TimeSpan time, TimeSpan totaltime)
        {
            if (totaltime >= new TimeSpan(1, 0, 0)) return (time.Hours < 10 ? "0" + time.Hours  : time.Hours.ToString()) + ":" + (time.Minutes < 10 ? "0" + time.Minutes  : time.Minutes.ToString()) + ":" + (time.Seconds < 10 ? "0" + time.Seconds : time.Seconds.ToString());
            else return (time.Minutes < 10 ? "0" + time.Minutes : time.Minutes.ToString()) + ":" + (time.Seconds < 10 ? "0" + time.Seconds : time.Seconds.ToString());
        }
        private string Position(TimeSpan position, TimeSpan Lenght)
        {
            double pos = position.TotalSeconds;
            double len = Lenght.TotalSeconds;
            int Coeff = (int)(Math.Round(pos / len, 1) * 100);
            switch (Coeff)
            {
                case(0):
                    return "https://i.ibb.co/k0XbRRj/Volume1.png";
                case (10):
                    return "https://i.ibb.co/Gd7ZShY/Volume20.png";
                case (20):
                    return "https://i.ibb.co/0p7wRp7/Volume40.png";
                case (30):
                    return "https://i.ibb.co/g61KvX2/Volume60.png";
                case (40):
                    return "https://i.ibb.co/QbcyKqN/Volume80.png";
                case (50):
                    return "https://i.ibb.co/6tH0P2K/Volume100.png";
                case (60):
                    return "https://i.ibb.co/N7QSGLS/Volume120.png";
                case (70):
                    return "https://i.ibb.co/mSKW12S/Volume140.png";
                case (80):
                    return "https://i.ibb.co/fCdmjP3/Volume160.png";
                case (90):
                    return "https://i.ibb.co/R0zvkjq/Volume180.png";
                case (100):
                    return "https://i.ibb.co/30pmR6B/Volume200.png";
            }
            return null;
        }
        public LavaTrack TrackPlaying
        {
            get
            {
                return _player.CurrentTrack;
            }
        }
        public TimeSpan TrackPosition
        {
            get
            {
                return _player.CurrentTrack.Position;
            }
        }
        public TimeSpan TrackLenght
        {
            get
            {
                return _player.CurrentTrack.Length;
            }
        }
        
        public async Task AddPositionAsync()
        {
            if (_player is null) return;
            TimeSpan time = new TimeSpan(0, 0, (int)(_player.CurrentTrack.Length.TotalSeconds * (PositionChange / 100.0)));
            if (!(_player.CurrentTrack.Position + time >= _player.CurrentTrack.Length))
                await _player.SeekAsync(_player.CurrentTrack.Position + time);

            _embed.Fields[1].Value = $"> [{Writetime(_player.CurrentTrack.Position, _player.CurrentTrack.Length)} / {Writetime(_player.CurrentTrack.Length, _player.CurrentTrack.Length)}]";
            _embed.ImageUrl = Position(_player.CurrentTrack.Position, _player.CurrentTrack.Length);
        }

        public async Task RemovePositionAsync()
        {
            if (_player is null) return;
            TimeSpan time = new TimeSpan(0, 0, (int)(_player.CurrentTrack.Length.TotalSeconds * (PositionChange / 100.0)));
            if (!(_player.CurrentTrack.Position - time <= new TimeSpan(0, 0, 0)))
                await _player.SeekAsync(_player.CurrentTrack.Position - time);

            _embed.Fields[1].Value = $"> [{Writetime(_player.CurrentTrack.Position, _player.CurrentTrack.Length)} / {Writetime(_player.CurrentTrack.Length, _player.CurrentTrack.Length)}]";
            _embed.ImageUrl = Position(_player.CurrentTrack.Position, _player.CurrentTrack.Length);
        }

        public async Task IncreaseVolumeAsync()
        {
            if (_player is null) return;
            int volume = _player.CurrentVolume + VolumeChange;
            if (volume > 150) volume = 150;
            await _player.SetVolumeAsync(volume);
            _embed.Fields[0].Value = $"> [ {_player.CurrentVolume} ]";
        }

        public async Task DecreaseVolumeAsync()
        {
            if (_player is null) return;
            int volume = _player.CurrentVolume - VolumeChange;
            if (volume < 2) volume = 2;
            await _player.SetVolumeAsync(volume);
            _embed.Fields[0].Value = $"> [ {_player.CurrentVolume} ]";
        }

        public async Task PauseOrResumeAsync()
        {
            if (_player is null) return;

            if (!_player.IsPaused)
                await _player.PauseAsync();
            
            else
                await _player.ResumeAsync();
        }

        public async Task SkipAsync()
        {
            Repeat = false;
            if (_player is null || _player.Queue.Items.Count() is 0) return;
            await _player.SkipAsync();
            await NewSong();
        }
        public async Task NewSong()
        {
            _embed.Author = new EmbedAuthorBuilder();
            _embed.Author.Name = _player.CurrentTrack.Author;
            _embed.Description = $"{_player.CurrentTrack.Title}\n[Открыть плейлист]({SiteURL}) | .q";//ссылка на видео { _player.CurrentTrack.Uri.ToString()}
            _embed.Author.IconUrl = _embed.ThumbnailUrl;
            _embed.ThumbnailUrl = await _player.CurrentTrack.FetchThumbnailAsync();
            _embed.Author.IconUrl = _embed.ThumbnailUrl;
        }
        public async Task<Embed> ControlEmbed()
        {
            await NewSong();
            _embed.Fields[1].Value = $"> [{Writetime(_player.CurrentTrack.Position, _player.CurrentTrack.Length)} / {Writetime(_player.CurrentTrack.Length, _player.CurrentTrack.Length)}]";
            _embed.ImageUrl = Position(_player.CurrentTrack.Position, _player.CurrentTrack.Length);
            return _embed.Build();
        }

        public ControlPanel(LavaPlayer player, SocketGuildUser provider)
        {
            Provider = provider;
            _player = player;
            _embed = new EmbedBuilder();
            _embed.Color = Color.DarkPurple;
            _embed.AddField("Громкость", $"> [ {_player.CurrentVolume} ]");
            _embed.AddField("Текущая позиция", $"> [{Writetime(_player.CurrentTrack.Position, _player.CurrentTrack.Length)} / {Writetime(_player.CurrentTrack.Length, _player.CurrentTrack.Length)}]");
            _embed.Footer = new EmbedFooterBuilder();
            _embed.Footer.Text = $"Управляющий - {Provider.Username}";
            _embed.ImageUrl = Position(_player.CurrentTrack.Position, _player.CurrentTrack.Length);
            Repeat = false;
        }
    }
}