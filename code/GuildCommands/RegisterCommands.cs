using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class RegisterCommands
{

    public static List<ulong> approvedUsers = new()
    {
        185851310308982804u, // Aurora
        558103204705861652u, // Gamation
        201779401044525056u // Bissy
    };
    
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("register_commands");
        command.WithDescription("Register all commands with Discord, for changes");

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
        if (approvedUsers.Contains(command.User.Id) || command.GuildId == 1167188182262095952)
        {
            await command.RespondAsync("Registering commands...");
            await program.CreateCommands();

        }
        else
        {
            await command.RespondAsync("This command is only for developers to reload the commands.");
        }
    }
}
