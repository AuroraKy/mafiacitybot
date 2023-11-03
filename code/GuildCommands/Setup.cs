using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class Setup
    {
        public static async Task CreateCommand(SocketGuild guild)
        {
            var command = new SlashCommandBuilder();
            command.WithName("setup");
            command.WithDescription("Command Description.");
            command.AddOption("hostrole", ApplicationCommandOptionType.Role, "change this later");
            command.AddOption("hostchannel", ApplicationCommandOptionType.Channel, "change this later");

            try
            {
                await guild.CreateApplicationCommandAsync(command.Build());
            }
            catch (HttpException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        public static async Task HandleCommand(SocketSlashCommand command, Program program)
        {
            try
            {
                Guild guild;
                if (program.guilds.TryGetValue(Convert.ToUInt64(command.GuildId), out guild))
                {
                    guild.HostRoleID = ((SocketRole)command.Data.Options.ElementAt(0).Value).Id;
                    guild.HostChannelID = ((SocketGuildChannel)command.Data.Options.ElementAt(1).Value).Id;
                    await command.RespondAsync("Guild setup has been modified.");
                }
                else
                {
                    guild = new Guild(Convert.ToUInt64(command.GuildId), ((SocketRole)command.Data.Options.ElementAt(0).Value).Id, ((SocketGuildChannel)command.Data.Options.ElementAt(1).Value).Id);
                    await program.AddGuild(guild);
                    await command.RespondAsync("Guild setup has been stored.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            
            
            
        }
    }
}