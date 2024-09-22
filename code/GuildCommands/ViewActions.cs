using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Numerics;

namespace mafiacitybot.GuildCommands;

public static class ViewActions
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithDefaultMemberPermissions(GuildPermission.ManageRoles);
        command.WithName("view_actions");
        command.WithDescription("(Host-Only) Views all current action");
        command.AddOption("clear", ApplicationCommandOptionType.Boolean, "Wether it should clear actions and letters on next phase change", isRequired: true);

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

        await command.RespondAsync($"It is currently {(guild.CurrentPhase == Guild.Phase.Day ? "Day" : "Night")}.");

        var channel = command.Channel;
        List<List<Embed>> queue = new List<List<Embed>>();
        List<Player> playersWithoutAction = new List<Player>();
        foreach (Player player in guild.Players)
        {
            Color embedColor = new((int)(player.PlayerID % 256), (int)(player.PlayerID / 1000 % 256), (int)(player.PlayerID / 1e6 % 256));
            IUser? user = await program.client.GetUserAsync(player.PlayerID);
            if(user == null) continue;
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
                        .WithAuthor(user)
                        .WithTitle($"Letter #{count} to {usertmp?.Username ?? " <@" + letter.recipientID + ">"}")
                        .WithDescription(letter.content)
                        .WithColor(embedColor));
                    count++;
                }
            }

            // seperate every letter/action
            List<Embed> toSend = new();

            if (player.Action.Length == 0)
            {
                playersWithoutAction.Add(player);
            } else
            {
                toSend.Add(embed.Build());
            }

            foreach (EmbedBuilder letter in letters)
            {
                toSend.Add(letter.Build());
            }
            // this doesn't work idk why.
            //toSend.Last().ToEmbedBuilder().WithTimestamp(DateTimeOffset.UtcNow).Build();

            queue.Add(toSend);
        }

        foreach(List<Embed> messages in queue) {
            foreach(Embed msg in messages)
            {
                await channel.SendMessageAsync(embed:msg);
                await Task.Delay(100);
            }
        }

        await channel.SendMessageAsync(embed:
            new EmbedBuilder()
                .WithAuthor("-- N/A -- ")
                .WithColor(Color.Red)
                .WithTitle($"Players without action")
                .WithDescription(String.Join(", ", playersWithoutAction.Select(player => player.Name)))
                .Build()
                );
        await Task.Delay(100);
        
        bool remove = (bool)command.Data.Options.First().Value;

        List<EmbedBuilder> hletters = new List<EmbedBuilder>();
        if (guild.hostLetters.Count > 0) {
            foreach (Guild.Letter letter in guild.hostLetters) {
                IUser? usertmp = program.client.GetUser(letter.recipientID);
                hletters.Add(new EmbedBuilder()
                    .WithAuthor("HOST LETTER")
                    .WithTitle($"Letter to {usertmp?.Username ?? " <@" + letter.recipientID + ">"}")
                    .WithDescription(letter.content)
                    .WithColor(Color.DarkerGrey));
            }
        }

        foreach (EmbedBuilder msg in hletters) {
            await channel.SendMessageAsync(embed: msg.Build());
            await Task.Delay(100);
        }

        guild.clearNextPhaseChange = remove;
        guild.Save();

        await channel.SendMessageAsync("All actions displayed!" + (remove ? " Actions will be cleared upon next use of phase." : ""));
    }
}
