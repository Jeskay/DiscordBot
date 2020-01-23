using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace DiscordBot.Services.Common
{
    public class VoteEmbed : IEmbeds
    {
        
        private SocketGuildUser _user;
        private LavaPlayer _player;
        public EmbedBuilder embedBuilder { get; }
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
        public async Task<Dictionary<ulong, VoteEmbed>> AddVote(SocketReaction reaction, ISocketMessageChannel messageChannel, Dictionary<ulong, VoteEmbed> TrackingVote)
        {
            if (reaction.Emote.Name != "\u2611") return TrackingVote;
            try
            {
                await channel.GetUserAsync(reaction.UserId);

            }
            catch (Exception)
            {
                return TrackingVote;
            }
            Votes++;
            var chnl = channel as SocketGuildChannel;
            int users = chnl.Users.Count - 1;
            if (Votes >= (users / 2.0))
            {
                await messageChannel.SendMessageAsync("", false, await Skip());
                TrackingVote.Remove(reaction.MessageId);
                await messageChannel.DeleteMessageAsync(reaction.MessageId);
            }
            return TrackingVote;
        }
        public async Task<Dictionary<ulong, VoteEmbed>> RemoveVote(SocketReaction reaction, Dictionary<ulong, VoteEmbed> TrackingVote)//семафор или мьютекс со статическим словарем
        {
            if (reaction.Emote.Name != "\u2611") return TrackingVote;
            try
            {
                await TrackingVote[reaction.MessageId].channel.GetUserAsync(reaction.UserId);
            }
            catch (Exception)
            {
                return TrackingVote;
            }
            TrackingVote[reaction.MessageId].Votes--;
            return TrackingVote;
        }
        public async Task<RestUserMessage> CreateEmbed(ISocketMessageChannel channel)
        {
            embedBuilder.Description = $"{_user.Mention} начал голосование за пропуск песни **{_player.Track.Title}**. \n Нажмите на реакцию для пропуска трека.";
            RestUserMessage msg = await channel.SendMessageAsync("", false, embedBuilder.Build());
            await msg.AddReactionAsync(new Emoji("\u2611"));
            return msg;
        }

        public async Task ModifyMessage(RestUserMessage message)//for future addons
        {
            await message.ModifyAsync(msg => { msg.Embed = embedBuilder.Build(); msg.Content = ""; });
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
