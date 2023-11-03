using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class Letter
    {
        public static async Task CreateCommand(SocketGuild guild)
        {
            var command = new SlashCommandBuilder();
            command.WithName("letter");
            command.WithDescription("Select the target and contents of the letter you wish to send.");
            command.AddOption("recipient", ApplicationCommandOptionType.String, "The person you wish to send the letter to.", isRequired: true);
            command.AddOption("letter", ApplicationCommandOptionType.String, "The contents of the letter.", isRequired: true);

            try
            {
                await guild.CreateApplicationCommandAsync(command.Build());
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