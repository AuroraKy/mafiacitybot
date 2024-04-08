using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Phase
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
        command.WithDefaultMemberPermissions(GuildPermission.ManageRoles);
        command.WithName("phase");
        command.WithDescription("Changes the phase from day to night (or vice versa).");

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

        await command.RespondAsync($"Changing to {(guild.CurrentPhase == Guild.Phase.Day ? "Night" : "Day")}.");

        var channel = command.Channel;
        List<List<Embed>> queue = new List<List<Embed>>();
        foreach (Player player in guild.Players)
        {
            Color embedColor = new((int)(player.PlayerID % 256), (int)(player.PlayerID / 1000 % 256), (int)(player.PlayerID / 1e6 % 256));
            EmbedBuilder embed = new EmbedBuilder()
                        .WithAuthor(program.client.GetUser(player.PlayerID))
                        .WithColor(embedColor)
                        .WithTitle($"{(guild.CurrentPhase == Guild.Phase.Day ? "Day" : "Night")} Action")
                        .WithDescription((player.Action.Length == 0 ? "None" : player.Action));


            List<EmbedBuilder> letters = new List<EmbedBuilder>();
            if(player.letters.Count > 0)
            {
                int count = 1;
                foreach (Player.Letter letter in player.letters)
                {
                    letters.Add(new EmbedBuilder()
                        .WithTitle($"Letter #{count} to {program.client.GetUser(letter.recipientID)?.Username ?? "<@" + letter.recipientID + ">"}")
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
            // this doesn't work idk why.
            toSend.Last().ToEmbedBuilder().WithTimestamp(DateTimeOffset.UtcNow).Build();

            queue.Add(toSend);
        }

        foreach(List<Embed> messages in queue) {
            foreach(Embed msg in messages)
            {
                await channel.SendMessageAsync(embed:msg);
                await Task.Delay(1500);
            }
        }

        foreach (Player player in guild.Players)
        {
            player.Action = "";
            player.letters = new();
        }
        guild.AdvancePhase();

        await channel.SendMessageAsync("It is now "+guild.CurrentPhase+".");
    }
}
