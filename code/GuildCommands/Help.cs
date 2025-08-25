using Discord;
using Discord.Net;
using Discord.WebSocket;
namespace mafiacitybot.GuildCommands;

public static class Help
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("help");
        command.WithDescription("Provides information about this bot and its commands.");

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

        EmbedBuilder embed = new EmbedBuilder()
                    .WithColor(new Color(255, 192, 255))
                    .WithTitle($"Welcome to the Mafia City Bot!")
                    .WithDescription("**General usage:**\nEvery *Phase*, use /action set to set your action - using *Abilities* or *Items*, *Croak Voting*, whatever. If it's a *Night Phase*, use /letter add <recipient> to send out a *Letter*.\n\nBelow is a list of all PLAYER commands.\n*All commands besides ping require a game to be setup by a Host*.")
                    .WithFields(new List<EmbedFieldBuilder>{ 
                        new EmbedFieldBuilder().WithName("Action").WithValue(@"*Action commands can only be used in your PLAYER channel.*

/action view - Displays your currently set action for you.
/action set - Opens a box to set an action containing what you will do this *Phase*.
/action clear - Removes your action by clearing it").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Letters").WithValue(@"*Letter commands can only be used at Night in your PLAYER channel.*

/letter view - Shows the first 180 characters of all *Letters* you have set to be sent this *Night*.
/letter view <number> - Shows the full text of *Letter* number <number>.
/letter add <recipient> - Opens a box to add a new *Letter* (up to 2000 chars) to PLAYER <recipient>.
/letter remove <number> - Removes *Letter* number <number>, preventing it from being sent out.
/letter edit <number> <recipient> - Edits *Letter* number <number> and changes its recipient to <recipient>.").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("ping").WithValue("/ping - Tells you the ping (response time) of the bot to Discord's servers.").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("info").WithValue("/info - Gives some information about the ongoing game.").WithIsInline(true),
                    });

        await command.RespondAsync(embed: embed.Build());
    }
}
