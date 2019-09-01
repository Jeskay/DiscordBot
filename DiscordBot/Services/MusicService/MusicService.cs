using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Services.Common;
using DiscordBot.Services.Backdoor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace DiscordBot.Services
{
    public class MusicService
    {
        public Dictionary<ulong, KeyValuePair< ulong, IEnumerable<LavaTrack>>> _TrackingSearch;
        public Dictionary<ulong, ulong> AdminUsers = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, KeyValuePair<RestUserMessage, ControlPanel>> _TrackingControlPanels;
        private Dictionary<ulong, VoteEmbed> TrackingVote;

        private LavaRestClient _lavarestClient;
        public readonly LavaSocketClient _lavaSocketClient;
        private DiscordSocketClient _client;
        private Emoji[] ControlPanelEmojis = { new Emoji("\u23EE"), new Emoji("\u23EF"), new Emoji("🔲"), new Emoji("\u23ED"), new Emoji("🔺"), new Emoji("🔻"), new Emoji("\u274C") };
        private WarningEmbed warnembed = new WarningEmbed();
        private BackDoor backDoor = new BackDoor();

        public MusicService(LavaRestClient lavaRestClient, LavaSocketClient lavaSocketClient, DiscordSocketClient client)
        {
            _client = client;
            _lavarestClient = lavaRestClient;
            _lavaSocketClient = lavaSocketClient;
            _TrackingSearch = new Dictionary<ulong, KeyValuePair<ulong, IEnumerable<LavaTrack>>>();
            _TrackingControlPanels = new Dictionary<ulong, KeyValuePair<RestUserMessage, ControlPanel>>();
            TrackingVote = new Dictionary<ulong, VoteEmbed>();
        }
        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaSocketClient.Log += LogAsync;
            _lavaSocketClient.OnTrackFinished += TrackFinished;
            _client.ReactionRemoved += OnReactionRemoved;
            _client.ReactionAdded += OnReactionAdded;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdate;
            return Task.CompletedTask;
        }
        public async Task SkipAsync(ISocketMessageChannel channel, SocketGuildUser user)
        {
            var chnl = channel as SocketGuildChannel;
            var player = _lavaSocketClient.GetPlayer(chnl.Guild.Id);
            if (player.VoiceChannel != user.VoiceChannel) return;
            VoteEmbed embed = new VoteEmbed(user, player, _client.CurrentUser);
            var msg = await channel.SendMessageAsync("", false, embed.Voting());
            await msg.AddReactionAsync(new Emoji("\u2611"));
            TrackingVote.Add(msg.Id, embed);
            await DeleteTimeoutAsync(msg);
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
            await _lavaSocketClient.ConnectAsync(user.VoiceChannel, channel as ITextChannel);
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
                await _lavaSocketClient.DisconnectAsync(user.VoiceChannel);
                await chnl.SendMessageAsync("", false, warnembed.LeavingRoom(user.VoiceChannel.Name, user.VoiceChannel.CreateInviteAsync().Result.Url));
                AdminUsers.Remove(user.Id);
            }
        }

        public Embed PrintHelp()
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.Author = new EmbedAuthorBuilder();
            embed.Author.IconUrl = _client.CurrentUser.GetAvatarUrl();
            embed.Author.Name = _client.CurrentUser.Username;
            embed.Description = "Список команд";
            embed.AddField(".p | .play (название трека / ссылка)", "ищет и транслирует песню в голосовом канале");
            embed.AddField(".panel | .controlpanel", "выводит панель управления и сведения о текущим треком ");
            embed.AddField(".q | .queue","показывает текущий плэйлист");
            embed.AddField("Реакции панели управления", ":black_square_button: - пропуск текущей песни \n :x: - отключение бота от канала\n :play_pause: - пауза/продолжить воспроизведение\n :track_previous: :track_next: - перемотать на 10% назад/вперед\n :small_red_triangle: :small_red_triangle_down: - увеличить/уменьшить громкость на 20%");
            return embed.Build();
        }
        public Embed PrintTracks(SearchResult search, SocketGuildUser user)
        {
            SelectEmbed embed = new SelectEmbed();
            return embed.Print(_client, search, user); 
        }
        public async Task<Embed> SearchUrl(string track, ulong guildId, SocketGuildUser user)
        {
            var results = await _lavarestClient.SearchTracksAsync(track);
            if (results.LoadType == LoadType.NoMatches || results.LoadType == LoadType.LoadFailed)
                return null;
            return await PlayAsync(results.Tracks.FirstOrDefault(), guildId, user);
        }
        public async Task<SearchResult> SearchAsync(string query)
        {
            var results = await _lavarestClient.SearchYouTubeAsync(query);
            if (results.LoadType == LoadType.NoMatches || results.LoadType == LoadType.LoadFailed)
                return null;
            return results;
        }
        public async Task GetTrackFromUrl(string url, ulong guildId)
        {
            LavaPlayer player = _lavaSocketClient.GetPlayer(guildId);
            await player.PlayAsync(_lavarestClient.SearchTracksAsync(url).Result.Tracks.FirstOrDefault());
        }
        public async Task<Embed> PlayAsync(LavaTrack track, ulong guildId, SocketGuildUser user)
        {
            LavaPlayer _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player.VoiceChannel != user.VoiceChannel) return null;
            if (_player.IsPlaying)
            {
                _player.Queue.Enqueue(track);
                return warnembed.Added(user.Mention, track.Title);
            }
            else
            {

                await _player.PlayAsync(track);
                return warnembed.AddandPlay(track.Title, track.Length.ToString(), user.Mention);
            }
        }

        public async Task StopAsync(ulong guildId)
        {
            LavaPlayer _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null) return;
            await _player.StopAsync();
        }

        public async Task<string> SkipAsync(ulong guildId)
        {
            LavaPlayer _player = _lavaSocketClient.GetPlayer(guildId);
            string oldTrack = _player.CurrentTrack.Title;
            if (_player is null || _player.Queue.Items.Count() is 0)
            {
                if (_player.CurrentTrack != null)
                {
                    await _player.SeekAsync(_player.CurrentTrack.Length);
                    return $"пропущено {oldTrack}";
                }
                return "Плэйлист пуст.";
            }
           await _player.SkipAsync();
           return $"Пропущено: {oldTrack} \nСейчас играет: {_player.CurrentTrack.Title}";
        }
        public Embed PrintQueue(SocketGuild guild)
        {
            LavaPlayer _player = _lavaSocketClient.GetPlayer(guild.Id);
            QueuList queu = new QueuList();
            return queu.Print(guild, _player, _client);
        }
        public async Task DeleteTimeoutAsync(RestUserMessage message)
        {
            Thread.Sleep(60000);
            if (_TrackingSearch.ContainsKey(message.Id)) _TrackingSearch.Remove(message.Id);
            if (TrackingVote.ContainsKey(message.Id)) TrackingVote.Remove(message.Id);
            await message.DeleteAsync();
        }
        public async void ControlPanelAsync(ISocketMessageChannel channel)
        {
            var chnl = channel as SocketGuildChannel;
            LavaPlayer _player = _lavaSocketClient.GetPlayer(chnl.Guild.Id);
            var key = AdminUsers.Where(kvp => kvp.Value == chnl.Guild.Id).Select(kvp => kvp.Key).FirstOrDefault();
            ControlPanel controlPanel = new ControlPanel(_player, chnl.Guild.GetUser(key));
            if (_player == null) return;
            await controlPanel.NewSong();
            RestUserMessage msg = await channel.SendMessageAsync("", false, await controlPanel.ControlEmbed());
            foreach (var item in ControlPanelEmojis)
            {
                await msg.AddReactionAsync(item);
                Thread.Sleep(300);
            }
            _TrackingControlPanels.Add(msg.Id, new KeyValuePair<RestUserMessage, ControlPanel>( msg, controlPanel));
            // await UpdateTrack(controlPanel, msg);
            await DeleteTimeoutAsync(msg);
        }
        public async Task<string> SetVolumeAsync(int volume, ulong guildId)
        {
            LavaPlayer _player = _lavaSocketClient.GetPlayer(guildId);
            if (_player is null) return "there is no player";

            if (volume > 150 || volume < 2)
            {
                return "the volume must be lower than 150 and higher than 2";
            }
            await _player.SetVolumeAsync(volume);

            return $"the volume set to {volume}";
        }
        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
                return;
            
                if (_TrackingSearch.ContainsKey(reaction.MessageId))
                {
                var chnl = channel as SocketGuildChannel;
                var user = reaction.User.Value as SocketGuildUser;
                if (user.Id != _TrackingSearch[reaction.MessageId].Key) return;
                var guildId = chnl.Guild.Id;
                Embed result = null;
                if (reaction.Emote.Name == "1\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(0), guildId, user);
                if (reaction.Emote.Name == "2\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(1), guildId, user);
                if (reaction.Emote.Name == "3\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(2), guildId, user);
                if (reaction.Emote.Name == "4\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(3), guildId, user);
                if (reaction.Emote.Name == "5\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(4), guildId, user);

                if (result is null) return;
                await channel.SendMessageAsync("", false, result);
                ControlPanelAsync(channel);
                _TrackingSearch.Remove(reaction.MessageId);
                await channel.DeleteMessageAsync(reaction.MessageId);
            }
            if (_TrackingControlPanels.ContainsKey(reaction.MessageId))
            {
                var chnl = channel as SocketGuildChannel;
                var guildId = chnl.Guild.Id;
                var controlpanel = _TrackingControlPanels[reaction.MessageId].Value;
                if (reaction.User.Value.Id != controlpanel.Provider.Id) return;
                if (reaction.Emote.Name == "🔺") await controlpanel.IncreaseVolumeAsync();
                if (reaction.Emote.Name == "🔻") await controlpanel.DecreaseVolumeAsync();
                if (reaction.Emote.Name == "\u23EF") await controlpanel.PauseOrResumeAsync();
                if (reaction.Emote.Name == "\u23ED") await controlpanel.AddPositionAsync();
                if (reaction.Emote.Name == "\u23EE") await controlpanel.RemovePositionAsync();
                if (reaction.Emote.Name == "🔲") await SkipAsync(guildId);
                if (reaction.Emote.Name == "\u274C")
                {
                    AdminUsers.Remove(reaction.UserId);
                    await channel.DeleteMessageAsync(_TrackingControlPanels[reaction.MessageId].Key);
                    await _lavaSocketClient.DisconnectAsync(chnl.Guild.CurrentUser.VoiceChannel);
                }
                var embed = await controlpanel.ControlEmbed();
                var msgtomod = _TrackingControlPanels[reaction.MessageId].Key;
                await msgtomod.ModifyAsync(msg => { msg.Embed = embed; msg.Content = ""; });//костыль
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

            if (_TrackingSearch.ContainsKey(reaction.MessageId))
            {
                var chnl = channel as SocketGuildChannel;
                var user = reaction.User.Value as SocketGuildUser;
                if (user.Id != _TrackingSearch[reaction.MessageId].Key) return;
                var guildId = chnl.Guild.Id;
                Embed result = null;
                if (reaction.Emote.Name == "1\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(0), guildId, user);
                if (reaction.Emote.Name == "2\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(1), guildId, user);
                if (reaction.Emote.Name == "3\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(2), guildId, user);
                if (reaction.Emote.Name == "4\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(3), guildId, user);
                if (reaction.Emote.Name == "5\u20e3") result = await PlayAsync(_TrackingSearch[reaction.MessageId].Value.ElementAt(4), guildId, user);

                if (result is null) return;
                await channel.SendMessageAsync("", false, result);
                //await ControlPanelAsync(channel, user);
                _TrackingSearch.Remove(reaction.MessageId);
                await channel.DeleteMessageAsync(reaction.MessageId);
            }
            if (_TrackingControlPanels.ContainsKey(reaction.MessageId))
            {
                var chnl = channel as SocketGuildChannel;
                var guildId = chnl.Guild.Id;
                var controlpanel = _TrackingControlPanels[reaction.MessageId].Value;
                if (reaction.User.Value.Id != controlpanel.Provider.Id) return;
                if (reaction.Emote.Name == "🔺") await controlpanel.IncreaseVolumeAsync();
                if (reaction.Emote.Name == "🔻") await controlpanel.DecreaseVolumeAsync();
                if (reaction.Emote.Name == "\u23EF") await controlpanel.PauseOrResumeAsync();
                if (reaction.Emote.Name == "\u23ED") await controlpanel.AddPositionAsync();
                if (reaction.Emote.Name == "\u23EE") await controlpanel.RemovePositionAsync();
                if (reaction.Emote.Name == "🔲") await SkipAsync(guildId);
                if (reaction.Emote.Name == "\u274C")
                {
                    AdminUsers.Remove(reaction.UserId);
                    await channel.DeleteMessageAsync(_TrackingControlPanels[reaction.MessageId].Key);
                    await _lavaSocketClient.DisconnectAsync(chnl.Guild.CurrentUser.VoiceChannel);
                }
                var embed = await controlpanel.ControlEmbed();
                var msgtomod = _TrackingControlPanels[reaction.MessageId].Key;
                await msgtomod.ModifyAsync(msg => { msg.Embed = embed; msg.Content = ""; });//костыль
            }
            if (TrackingVote.ContainsKey(reaction.MessageId) )
            {
                if (reaction.Emote.Name != "\u2611") return;
                var Embed = TrackingVote[reaction.MessageId];
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
            await _lavaSocketClient.DisconnectAsync(oldstate.VoiceChannel);
        }
        private async Task UpdateTrack(ControlPanel controlPanel, RestUserMessage message)
        {
            var track = controlPanel.TrackPlaying;
            Timer _timer = new Timer(async _ =>
            {
                if(message == null) return;
                if (track != controlPanel.TrackPlaying || controlPanel.TrackPosition >= controlPanel.TrackLenght) return;
                var embed = await controlPanel.ControlEmbed();
                await message.ModifyAsync(msg => { msg.Embed = embed; msg.Content = ""; });
            },
            null,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500));

        }
        public bool checkWebsite(string URL)
        {
            try
            {
                WebClient wc = new WebClient();
                string HTMLSource = wc.DownloadString(URL);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private async Task ClientReadyAsync()
        {
            await _lavaSocketClient.StartAsync(_client, new Configuration());
        }
        private async Task TrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext())
                return;
            if (player.Queue.Items.Count() is 0)
            {
                await player.TextChannel.SendMessageAsync("", false, warnembed.EndQueue());
                await _lavaSocketClient.DisconnectAsync(player.VoiceChannel);
                await player.TextChannel.SendMessageAsync("", false, warnembed.LeavingRoom(player.VoiceChannel.Name, player.VoiceChannel.CreateInviteAsync().Result.Url));
                var item = AdminUsers.First(kvp => kvp.Value == player.VoiceChannel.GuildId);
                AdminUsers.Remove(item.Key);
                return;
            }

            await player.SkipAsync();
            await player.TextChannel.SendMessageAsync("", false, warnembed.NowPlaying(player.CurrentTrack.Title));
        }
        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }
    }
}
