﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Services.Common;
using DiscordBot.Services.Backdoor;
using DiscordBot.Services.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace DiscordBot.Services
{
    public class MusicService
    {
        public Dictionary<ulong, ulong> AdminUsers = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, KeyValuePair<RestUserMessage, ControlPanel>> _TrackingControlPanels;
        private Dictionary<ulong, VoteEmbed> TrackingVote;

        private readonly LavaNode _lavaNode;
        private DiscordSocketClient _client;
        private readonly WarningEmbed warnembed = new WarningEmbed();
        private HelpEmbed helpEmbed;
        private readonly SelectEmbed selectEmbed = new SelectEmbed();
        private readonly QueueList queueEmbed = new QueueList();
        private readonly BackDoor backDoor = new BackDoor();

        public MusicService(LavaNode lavaNode, DiscordSocketClient client)
        {
            _client = client;
            _lavaNode = lavaNode;
            _TrackingControlPanels = new Dictionary<ulong, KeyValuePair<RestUserMessage, ControlPanel>>();
            TrackingVote = new Dictionary<ulong, VoteEmbed>();
        }
        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaNode.OnLog += LogAsync;
            _lavaNode.OnTrackEnded += TrackFinished;
            _client.ReactionRemoved += OnReactionRemoved;
            _client.ReactionAdded += OnReactionAdded;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdate;

            return Task.CompletedTask;
        }
        public async Task SkipAsync(ISocketMessageChannel channel, SocketGuildUser user)
        {
            var chnl = channel as SocketGuildChannel;
            var player = _lavaNode.GetPlayer(chnl.Guild);
            if (player.VoiceChannel != user.VoiceChannel) return;
            VoteEmbed embed = new VoteEmbed(user, player, _client.CurrentUser);
            RestUserMessage message = await embed.CreateEmbed(channel);
            TrackingVote.Add(message.Id, embed);
            await DeleteTimeoutAsync(message);
        }
        public bool CheckMessage(SocketUserMessage msg)
        {
            return backDoor.CheckMessage(msg);
        }

        public async Task<bool> JoinAsync(SocketGuildUser user, ISocketMessageChannel chnl)
        {
            var channel = chnl as SocketGuildChannel;
            RestUserMessage msg = null;
            if (AdminUsers.ContainsValue(channel.Guild.Id))
            {
                if (user.VoiceChannel != channel.Guild.CurrentUser.VoiceChannel)
                {
                    msg = await chnl.SendMessageAsync("", false, warnembed.IsUsing(channel.Guild.CurrentUser.VoiceChannel));
                    return false;
                }
                else return true;
            }
            if (user.VoiceChannel is null)
            {
                msg = await chnl.SendMessageAsync("", false, warnembed.ShouldbeInVoice());
                return false;
            }
            await _lavaNode.JoinAsync(user.VoiceChannel, channel as ITextChannel);
            await Task.Delay(500);
            if (channel.Guild.CurrentUser.VoiceChannel == null)
            {
                await chnl.SendMessageAsync("", false, warnembed.NotEnoughPermission(_client));
                return false;
            }
            if (msg != null) await DeleteTimeoutAsync(msg);
            AdminUsers.Add(user.Id, channel.Guild.Id);


            return true;
        }
        public async Task LeaveAsync(SocketGuildUser user, ISocketMessageChannel chnl)
        {
            var channel = chnl as SocketGuildChannel;
            if (!AdminUsers.ContainsValue(channel.Guild.Id))
            {
                await chnl.SendMessageAsync("", false, warnembed.NoUserPermission(user.Mention));
                return;
            }
            if(!AdminUsers.ContainsKey(user.Id))
            {
                await chnl.SendMessageAsync("", false, warnembed.NoUserPermission(user.Mention));
                return;
            }
            if (user.VoiceChannel is null)
            {
                await chnl.SendMessageAsync("", false, warnembed.NoUserPermission(user.Mention));
                return;
            }
            else
            {
                await _lavaNode.LeaveAsync(user.VoiceChannel);
                await chnl.SendMessageAsync("", false, warnembed.LeavingRoom(user.VoiceChannel.Name, user.VoiceChannel.CreateInviteAsync().Result.Url));
                AdminUsers.Remove(user.Id);
            }
        }
        public async Task PrintHelp(ISocketMessageChannel messageChannel)
        {
            await helpEmbed.CreateEmbed(messageChannel);
        }
        public async Task PrintTracks(IReadOnlyList<LavaTrack> search, SocketGuildUser user, ISocketMessageChannel messageChannel)
        {
            selectEmbed.Update(_client.CurrentUser ,search, user);
            
            RestUserMessage message = await selectEmbed.CreateEmbed(messageChannel);
            selectEmbed.AddSelection(message.Id, search);
            await DeleteTimeoutAsync(message);
        }
        public async Task<IReadOnlyList<LavaTrack>> SearchAsync(string query)
        {
            var results = await _lavaNode.SearchYouTubeAsync(query);
            if (results.LoadStatus == LoadStatus.NoMatches || results.LoadStatus == LoadStatus.LoadFailed)
                return null;
            return results.Tracks;
        }
        public async Task<Embed> PlayAsync(LavaTrack track, IGuild guild, SocketGuildUser user)
        {
            LavaPlayer _player = _lavaNode.GetPlayer(guild);
            if (_player.VoiceChannel != user.VoiceChannel) return null;
            if (_player.PlayerState == PlayerState.Playing)
            {
                _player.Queue.Enqueue(track);
                return warnembed.Added(user.Mention, track.Title);
            }
            else
            {

                await _player.PlayAsync(track);
                await _player.UpdateVolumeAsync(100);
                return warnembed.AddandPlay(track.Title, track.Duration.ToString(), user.Mention);
            }
        }
        public async Task<string> SkipAsync(IGuild guild)
        {
            LavaPlayer _player = _lavaNode.GetPlayer(guild);
            string oldTrack = _player.Track.Title;
            if (_player is null || _player.Queue.Items.Count() is 0)
            {
                if (_player.Track != null)
                {
                    await _player.SeekAsync(_player.Track.Duration);
                    return $"пропущено {oldTrack}";
                }
                return "Плэйлист пуст.";
            }
           await _player.SkipAsync();
           return $"Пропущено: {oldTrack} \nСейчас играет: {_player.Track.Title}";
        }
        public async Task PrintQueue(SocketGuild guild, ISocketMessageChannel messageChannel)
        {
            LavaPlayer _player = _lavaNode.GetPlayer(guild);
            queueEmbed.UpdateQueue(guild, _player, _client.CurrentUser);
            await queueEmbed.CreateEmbed(messageChannel);
        }
        public async Task DeleteTimeoutAsync(RestUserMessage message)
        {
            Thread.Sleep(60000);
            if (TrackingVote.ContainsKey(message.Id)) TrackingVote.Remove(message.Id);
            selectEmbed.RemoveSelection(message.Id);
            await message.DeleteAsync();
        }
        public async void ControlPanelAsync(ISocketMessageChannel channel)
        {
            var chnl = channel as SocketGuildChannel;
            LavaPlayer _player = _lavaNode.GetPlayer(chnl.Guild);
            var key = AdminUsers.Where(kvp => kvp.Value == chnl.Guild.Id).Select(kvp => kvp.Key).FirstOrDefault();
            ControlPanel controlPanel = new ControlPanel(_player, chnl.Guild.GetUser(key));
            if (_player == null) return;
            RestUserMessage msg = await controlPanel.CreateEmbed(channel);
            _TrackingControlPanels.Add(msg.Id, new KeyValuePair<RestUserMessage, ControlPanel>( msg, controlPanel));
             //await UpdateTrack(controlPanel, msg);
            await DeleteTimeoutAsync(msg);
        }
        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
                return;

            var selectedTrack = selectEmbed.CheckSelection(reaction.User.Value as SocketGuildUser, reaction);
            if ( selectedTrack != null)
            {
                var embed = await PlayAsync(selectedTrack, (channel as SocketGuildChannel).Guild, reaction.User.Value as SocketGuildUser);
                await channel.SendMessageAsync("", false, embed);
                ControlPanelAsync(channel);
                selectEmbed.RemoveSelection(reaction.MessageId);
                await channel.DeleteMessageAsync(reaction.MessageId);
            }
            if (_TrackingControlPanels.ContainsKey(reaction.MessageId))
            {
                var controlPanel = _TrackingControlPanels[reaction.MessageId].Value;
                AdminUsers = await controlPanel.CheckCommand(channel, reaction, _TrackingControlPanels[reaction.MessageId].Key, _lavaNode, AdminUsers);
            }
            if (TrackingVote.ContainsKey(reaction.MessageId))
            {
                var Embed = TrackingVote[reaction.MessageId];
                if (reaction.Emote.Name != "\u2611") return;
                try
                {
                    await Embed.channel.GetUserAsync(reaction.UserId);

                }
                catch (Exception)
                {
                    return;
                }
                Embed.Votes++;
                var chnl = Embed.channel as SocketGuildChannel;
                int users = chnl.Users.Count - 1;
                if (Embed.Votes >= ( users / 2.0))
                {
                    await channel.SendMessageAsync("", false, await Embed.Skip());
                    TrackingVote.Remove(reaction.MessageId);
                    await channel.DeleteMessageAsync(reaction.MessageId);
                }
            }
        }
        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
                return;

            var selectedTrack = selectEmbed.CheckSelection(reaction.User.Value as SocketGuildUser, reaction);
            if (selectedTrack != null)
            {
                var embed = await PlayAsync(selectedTrack, (channel as SocketGuildChannel).Guild, reaction.User.Value as SocketGuildUser);
                await channel.SendMessageAsync("", false, embed);
                ControlPanelAsync(channel);
                selectEmbed.RemoveSelection(reaction.MessageId);
                await channel.DeleteMessageAsync(reaction.MessageId);
            }

            if (_TrackingControlPanels.ContainsKey(reaction.MessageId))
            {
                var controlPanel = _TrackingControlPanels[reaction.MessageId].Value;
                AdminUsers = await controlPanel.CheckCommand(channel, reaction, _TrackingControlPanels[reaction.MessageId].Key, _lavaNode, AdminUsers);

            }
            if (TrackingVote.ContainsKey(reaction.MessageId) )
            {
                if (reaction.Emote.Name != "\u2611") return;
                try
                {
                    await TrackingVote[reaction.MessageId].channel.GetUserAsync(reaction.UserId);

                }
                catch (Exception)
                {
                    return;
                }
                TrackingVote[reaction.MessageId].Votes--;
            }
        }
        private async Task UserVoiceStateUpdate(SocketUser user, SocketVoiceState oldstate, SocketVoiceState newState)
        {
            if (!AdminUsers.ContainsKey(user.Id)) return;
            if (oldstate.VoiceChannel == newState.VoiceChannel) return;
            AdminUsers.Remove(user.Id);
            await _lavaNode.LeaveAsync(oldstate.VoiceChannel);
        }
        private async Task UpdateTrack(ControlPanel controlPanel, RestUserMessage message)//not used right now
        {
            Timer _timer = new Timer(async _ =>
            {
                if(message == null) return;
                try
                {
                    await controlPanel.ModifyMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
            },
            null,
            TimeSpan.FromMilliseconds(5000),
            TimeSpan.FromMilliseconds(5000));

        }
        private async Task ClientReadyAsync()
        {
            await _lavaNode.ConnectAsync();
            helpEmbed = new HelpEmbed(_client.CurrentUser);
        }
        private async Task TrackFinished(TrackEndedEventArgs endedEventArgs)
        {
            if (!endedEventArgs.Reason.ShouldPlayNext())
                return;
            LavaPlayer player = endedEventArgs.Player;
            if (player.Queue.Items.Count() is 0)
            {
                await player.TextChannel.SendMessageAsync("", false, warnembed.EndQueue());
                await _lavaNode.LeaveAsync(player.VoiceChannel);
                await player.TextChannel.SendMessageAsync("", false, warnembed.LeavingRoom(player.VoiceChannel.Name, player.VoiceChannel.CreateInviteAsync().Result.Url));
                var item = AdminUsers.First(kvp => kvp.Value == player.VoiceChannel.GuildId);
                AdminUsers.Remove(item.Key);
                return;
            }

            await player.SkipAsync();
            await player.TextChannel.SendMessageAsync("", false, warnembed.NowPlaying(player.Track.Title));
        }
        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }
    }
}
