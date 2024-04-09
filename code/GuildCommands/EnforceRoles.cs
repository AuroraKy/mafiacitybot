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
        command.AddOption("role", ApplicationCommandOptionType.Role, "The role you want to enforce on players", isRequired: true);

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

        SocketGuildChannel chl = command.Channel as SocketGuildChannel;
        SocketGuild g = chl.Guild;
        var role = command.Data.Options.First().Value;

        List<string> added = new();
        List<string> removed = new();

        await g.DownloadUsersAsync();

        foreach(SocketGuildUser user in g.Users)
        {
            if(guild.Players.Find(p => p.PlayerID == user.Id) != null)
            {
                if(!user.Roles.Contains(role))
                {
                    await user.AddRoleAsync((IRole)role);
                    added.Add(user.DisplayName);
                }
            } 
            else
            {
                if (user.Roles.Contains(role))
                {
                    await user.RemoveRoleAsync((IRole)role);
                    removed.Add(user.DisplayName);
                }
            }
        }

        await command.RespondAsync($"Role added to: {String.Join(", ", added)}\nRole removed from: {String.Join(", ", removed)}");
    }
}
