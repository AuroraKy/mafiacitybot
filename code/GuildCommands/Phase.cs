using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Phase
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
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

        await command.RespondAsync($"Changing to {(guild.CurrentPhase == Guild.Phase.Day ? "Night" : "Day")}.\n------------- DATA -------------");

        var channel = command.Channel;
        string answer = "";
        foreach (Player player in guild.Players)
        {
            answer += "-- Player: " + player.Name + " -- \n";
            answer += "- "+(guild.CurrentPhase == Guild.Phase.Day ? "Day" : "Night") + " Action:\n";
            answer += $"{player.Action}\n\n";
            if(guild.CurrentPhase == Guild.Phase.Day)
            {
                answer += $"- Letters:\n";
                int count = 1;
                foreach (Player.Letter letter in player.letters)
                {
                    answer += $"Letter #{count} to {program.client.GetUser(letter.recipientID)?.Username ?? "<@"+letter.recipientID+">"}:\n{letter.content}\n\n";
                    count++;
                }
            }
            answer += "-- End of player " + player.Name + " --\n";
        }

        for(int i = 0; i < Math.Ceiling(answer.Length/2000f); i++)
        {
            await channel.SendMessageAsync(String.Concat("", answer.AsSpan(i * 2000, Math.Min(answer.Length-(i*2000), 2000))));
            await Task.Delay(1500);
        }

        foreach (Player player in guild.Players)
        {
            player.Action = "";
            player.letters = new();
        }
        guild.AdvancePhase();

        await channel.SendMessageAsync("---- Done! ----\n It is now "+guild.CurrentPhase+".");
    }
}
