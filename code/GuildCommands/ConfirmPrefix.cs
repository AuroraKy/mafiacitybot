using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class ConfirmPrefix
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("confirm_prefix");
        command.WithDescription("Confirm you want to don this mask.");
        command.AddOption("id", ApplicationCommandOptionType.Integer, "The Ballroom number to don the mask for.", isRequired: true);

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
        
        var user = (SocketGuildUser)command.User;
        var id = Convert.ToInt32(command.Data.Options.First(o => o.Name == "id").Value);

        if (!guild.MasqueradeTunnels.TryGetValue(id, out var tunnel))
        {
            await command.RespondAsync("No ballroom has that number.");
            return;
        }

        if (!tunnel.PendingPrefixConfirmations.TryGetValue(user.Id, out var prefix))
        {
            await command.RespondAsync("You have yet to decide on a mask." +
                                       "\n*Run \\mask with the prefix option filled in first.*");
            return;
        }

        tunnel.Prefixes[user.Id] = prefix;
        tunnel.PendingPrefixConfirmations.Remove(user.Id, out _);

        await command.RespondAsync($"You don the mask of **{prefix}**. The doors of the Masquerade will open for you." +
                                   $"\n*Run \\mask again with no prefix option to start talking.*");
    }
}
