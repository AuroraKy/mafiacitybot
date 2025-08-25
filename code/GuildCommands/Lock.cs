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
        command.WithDescription("(Host-Only) Toggles the lock on actions and *Letters*.");
        command.AddOption("announce", ApplicationCommandOptionType.Boolean,
                "Whether to announce the lock toggle in all PLAYER channels.",
                false, true)
            ;        
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
            await command.RespondAsync($"You must have the HOST role to use this command!");
            return;
        }
        if (guild.HostChannelID != command.ChannelId)
        {
            await command.RespondAsync($"Command must be executed in the HOST bot channel!");
            return;
        }

        guild.isLocked = !guild.isLocked;

        await command.RespondAsync("Actions and *Letter* commands are now " + (guild.isLocked ? "locked, and cannot be used until unlocked." : "unlocked and can be used."));
        
        if ((bool)command.Data.Options.First().Value)
        {
            foreach (Player player in guild.Players)
            { 
                var playerChannel = await program.client.GetChannelAsync(player.ChannelID) as ITextChannel;
                await playerChannel.SendMessageAsync("*Actions and Letter commands are now " + (guild.isLocked ? "locked. Hold tight for the Phase transition!" : "unlocked and can be used. Good luck."));
            }
        }

        guild.Save();
        
    }
}

}