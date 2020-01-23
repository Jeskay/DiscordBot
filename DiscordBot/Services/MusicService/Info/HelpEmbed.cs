using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Services.Info
{
    public class HelpEmbed : IEmbeds
    {
        public EmbedBuilder embedBuilder { get; }

        private readonly Dictionary<string, string> Commands = new Dictionary<string, string>();
        private readonly string UserAvatarUrl;
        private readonly string Username;
        public async Task<RestUserMessage> CreateEmbed(ISocketMessageChannel messageChannel)
        {
            embedBuilder.Author = new EmbedAuthorBuilder();
            embedBuilder.Author.IconUrl = UserAvatarUrl;
            embedBuilder.Author.Name = Username;
            embedBuilder.Description = "Список команд";

            foreach (var command in Commands)
                embedBuilder.AddField(command.Key, command.Value); 
            
            embedBuilder.AddField("Реакции панели управления", ":black_square_button: - пропуск текущей песни \n :x: - отключение бота от канала\n :play_pause: - пауза/продолжить воспроизведение\n :track_previous: :track_next: - перемотать на 10% назад/вперед\n :small_red_triangle: :small_red_triangle_down: - увеличить/уменьшить громкость на 20%");

            return await messageChannel.SendMessageAsync("", false, embedBuilder.Build());
        }

        public Task ModifyMessage(RestUserMessage message)
        {
            throw new NotImplementedException();//coming soon
        }

        public HelpEmbed(SocketSelfUser selfUser)
        {
            embedBuilder = new EmbedBuilder();
            Username = selfUser.Username;
            UserAvatarUrl = selfUser.GetAvatarUrl();
            Commands.Add(".p | .play (название трека / ссылка)", "ищет и транслирует песню в голосовом канале");
            Commands.Add(".panel | .controlpanel",               "выводит панель управления и сведения о текущим треком ");
            Commands.Add(".q | .queue",                          "показывает текущий плэйлист");
        }
    }
}
