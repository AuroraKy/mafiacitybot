using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class EnforceRoles
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
        command.WithName("enforce_roles");
        command.WithDescription("(Host-Only) Makes sure a certain role is given to all active players and removed from everyone else.");
        command.AddOption("player", ApplicationCommandOptionType.Role, "The role you want to enforce", isRequired: true);

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
        if (!program.guilds.TryGetValue(Convert.ToUInt64(command.GuildId), out Guild guild))
        {
            await command.RespondAsync($"You must use setup before being able to use this command!");
            return;
        }
        if (!Guild.IsHostRoleUser(command, guild.HostRoleID))
        {
            await command.RespondAsync($"You must have the host role to use this command!");
            return;
        }


        await command.RespondAsync($"Role added to:\nRole removed from:");
    }
}
