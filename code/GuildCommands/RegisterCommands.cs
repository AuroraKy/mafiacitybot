using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class RegisterCommands
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
        command.WithName("register_commands");
        command.WithDescription("Register all commands with discord, for changes");

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
        if(command.User.Id != 185851310308982804u)
        {
            await command.RespondAsync("This command is just for Aurora to reload the commands.");
            return;
        }
        await command.RespondAsync("Registering commands...");
        program.CreateCommands();
    }
}
