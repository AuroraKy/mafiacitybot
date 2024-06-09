using Discord;
using Discord.Net;
using Discord.WebSocket;
namespace mafiacitybot.GuildCommands;

public static class Help
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
        command.WithName("help");
        command.WithDescription("Provides information about this bot and it's commands.");

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

        EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(new Color(255, 192, 255))
                    .WithTitle($"Mafia Bot Help")
                    .WithDescription("General usage:\n**Every day or night use /action set to set your action for that phase, use /letter add <recipient> to send out letters at night.**\n\nFull info for commands that are not host-only.\nAll commands besides ping require a game to be setup by a host.")
                    .WithFields(new List<EmbedFieldBuilder>{ 
                        new EmbedFieldBuilder().WithName("Action").WithValue(@"Can only be used in your player channel.

/action view - Displays your current action in chat
/action set - Opens a modal to set an action (up to 2000 chars) that represents what you will do today/tonight. Will post the first 180 characters of it in chat to confirm.
/action clear - Removes your action by clearing it").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Letters").WithValue(@"Can only be used at night in your player channel.
This lets you add any amount of letters you want, usually you can only send one.

/letter view - Shows the first 180 characters of all letters you have set to be send tonight
/letter view <nummer> - Shows the full text of letter nummer <nummer>
/letter add <recipient> - Opens a modal to add a new letter (up to 2000 chars) to user <recipient>. Will post the first 180 characters of it in chat to confirm.
/letter remove <nummer> - Removes letter nummer <nummer>
/letter edit <nummer> <recipient> - Edits letter nummer <nummer>, also changes recipient to <recipient>. Will post the first 180 characters of it in chat to confirm.").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("ping").WithValue("/ping - Tells you the ping of the bot to discord").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("info").WithValue("/info - Gives some information about this game").WithIsInline(true),
                    });

        await command.RespondAsync(embed: embed.Build());
    }
}
