using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace mafiacitybot.GuildCommands
{
public static class Lock
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("lock");
        command.WithDescription("(Host-Only) Locks/unlocks all actions and letters.");
        
        try
        {
            if (guild != null) {
                await guild.CreateApplicationCommandAsync(command.Build());
            } else {
                await client.CreateGlobalApplicationCommandAsync(command.Build());
            }
        }
        catch (ApplicationCommandException exception)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(json);
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
        if (guild.HostChannelID != command.ChannelId)
        {
            await command.RespondAsync($"Command must be executed in the host channel!");
            return;
        }

        guild.isLocked = !guild.isLocked;

        await command.RespondAsync("Actions and letter commands are now " + (guild.isLocked ? "locked, and cannot be used until unlocked." : "unlocked and can be used."));
        guild.Save();
        
    }
}

}