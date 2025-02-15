using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Numerics;

namespace mafiacitybot.GuildCommands;

public static class ViewAction
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithDefaultMemberPermissions(GuildPermission.ManageRoles);
        command.WithName("view_action");
        command.WithDescription("(Host-Only) Views action of a specific player");
        command.AddOption("player", ApplicationCommandOptionType.User, "Player to view action on", isRequired: true);

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
        if (guild.HostChannelID != command.ChannelId)
        {
            await command.RespondAsync($"Command must be executed in the host channel!");
            return;
        }

        SocketGuildUser user = (SocketGuildUser)command.Data.Options.First().Value;

        var channel = command.Channel;
        Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
        if (player == null)
        {
            _ = command.RespondAsync($"Player {user.Nickname} ({user.DisplayName}) not found");
            return;
        }
        Color embedColor = new((int)(player.PlayerID % 256), (int)(player.PlayerID / 1000 % 256), (int)(player.PlayerID / 1e6 % 256));

        EmbedBuilder embed = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithColor(player.Action.Length == 0 ? Color.Red : embedColor)
                    .WithTitle($"{(guild.CurrentPhase == Guild.Phase.Day ? "Day" : "Night")} Action")
                    .WithDescription((player.Action.Length == 0 ? "N/A" : player.Action));


        List<EmbedBuilder> letters = new List<EmbedBuilder>();
        if(player.letters.Count > 0)
        {
            int count = 1;
            foreach (Player.Letter letter in player.letters)
            {
                IUser? usertmp = program.client.GetUser(letter.recipientID);
                letters.Add(new EmbedBuilder()
                    .WithTitle($"Letter #{count} to {usertmp?.Username ?? " <@" + letter.recipientID + ">"}")
                    .WithDescription(letter.content)
                    .WithColor(embedColor));
                count++;
            }
        }

        // seperate every letter/action
        List<Embed> toSend = new()
        {
            embed.Build()
        };

        foreach (EmbedBuilder letter in letters)
        {
            toSend.Add(letter.Build());
        }

        foreach (Embed msg in toSend)
        {
            await channel.SendMessageAsync(embed: msg);
            await Task.Delay(100);
        }

        await command.RespondAsync($"Done!", ephemeral: true);
    }
}
