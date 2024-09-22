using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class Whisper
    {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("commandname");
            command.WithDescription("Command Description.");

            try
            {
                if (guild != null) {
                    await guild.CreateApplicationCommandAsync(command.Build());
                } else {
                    await client.CreateGlobalApplicationCommandAsync(command.Build());
                }
            }
            catch (HttpException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        public static async Task HandleCommand(SocketSlashCommand command)
        {
            await command.RespondAsync("Command output.");
        }
    }
}