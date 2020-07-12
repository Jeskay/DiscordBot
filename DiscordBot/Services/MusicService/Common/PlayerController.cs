using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace DiscordBot.Services.Common
{
    class PlayerController
    {
        private readonly LavaPlayer player;
        private const int PositionChange = 10;
        private const int VolumeChange = 20;
        #region Properties
        public int Volume
        {
            get 
            {
                return player.Volume;
            }
        }
        public LavaTrack Track
        {
            get
            {
                return player.Track;
            }
        }
        public TimeSpan TrackPosition
        {
            get
            {
                return player.Track.Position;
            }
        }
        public TimeSpan TrackLenght
        {
            get
            {
                return player.Track.Duration;
            }
        }
        #endregion
        public async Task<bool> AddPositionAsync()
        {
            if (player is null) return false;
            TimeSpan time = new TimeSpan(0, 0, (int)(player.Track.Duration.TotalSeconds * (PositionChange / 100.0)));
            if (!(player.Track.Position + time >= player.Track.Duration))
                await player.SeekAsync(player.Track.Position + time);
            return true;
        }

        public async Task<bool> RemovePositionAsync()
        {
            if (player is null) return false;
            TimeSpan time = new TimeSpan(0, 0, (int)(player.Track.Duration.TotalSeconds * (PositionChange / 100.0)));
            if (!(player.Track.Position - time <= new TimeSpan(0, 0, 0)))
                await player.SeekAsync(player.Track.Position - time);
            return true;
        }

        public async Task<bool> IncreaseVolumeAsync()
        {
            if (player is null) return false;
            ushort volume = Convert.ToUInt16(player.Volume + VolumeChange);
            if (volume > 150) volume = 150;
            await player.UpdateVolumeAsync(volume);
            return true;
        }

        public async Task<bool> DecreaseVolumeAsync()
        {
            if (player is null) return false;
            ushort volume = Convert.ToUInt16(player.Volume - VolumeChange);
            if (volume < 2) volume = 2;
            await player.UpdateVolumeAsync(volume);
            return true;
        }

        public async Task<bool> PauseOrResumeAsync()
        {
            if (player is null) return false;

            if (player.PlayerState != PlayerState.Paused)
                await player.PauseAsync();

            else
                await player.ResumeAsync();
            return true;
        }

        public async Task<bool> SkipAsync()
        {
            if (player is null || player.Queue.Count() is 0) return false;
            await player.SkipAsync();
            return true;
        }
        public PlayerController(LavaPlayer player)
        {
            this.player = player;
        }
    }
}
