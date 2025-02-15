using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class ClearPlayer
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("clear_player");
        command.WithDescription("(Host-Only) Clears a player.");
        command.AddOption("player", ApplicationCommandOptionType.User, "The player you want to remove", isRequired: true);

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
        SocketGuildUser user = (SocketGuildUser)command.Data.Options.ElementAt(0).Value;
        ulong userID = (user).Id;
        Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
        if(player != null)
        {
            if(player.PlayerID != userID) {
                player.LinkedIDs.Remove(userID);
                var name = player.LinkedNames[userID];
                player.LinkedNames.Remove(userID);
                await command.RespondAsync($"Removed {name} link from player {player.Name}");
            } else {
                guild.Players.Remove(player);
                await command.RespondAsync($"Removed {player.Name} {(player.LinkedNames.Count > 0 ? $" (Also: {String.Join(", ", player.LinkedNames.Select(x => x.Value))}) " : "")}(<@{player.PlayerID}> <#{player.ChannelID}>) from players");
            }
            guild.Save();
        } else
        {
            await command.RespondAsync($"Player {user.DisplayName} not found as a registered user.");
        }

    }
}
