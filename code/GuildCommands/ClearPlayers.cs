using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class ClearPlayers
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("clear_players");
        command.WithDescription("(Host-Only) Clears all players.");

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

        guild.Players = new();
        await command.RespondAsync("Cleared all players!");
        guild.Save();
    }
}
