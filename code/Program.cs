using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.Json;
using mafiacitybot.GuildCommands;
using System.Reflection;

namespace mafiacitybot;

public class Program
{
    public DiscordSocketClient client = new DiscordSocketClient();
    public Settings settings;
    public Dictionary<ulong, Guild> guilds = new Dictionary<ulong, Guild>();
    public SlashCommandHandler slashCommandHandler;

    public Program()
    {
        try
        {
            string text = File.ReadAllText("../../../../Data/settings.json");
            settings = JsonSerializer.Deserialize<Settings>(text);
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine("Failed to read settings.json");
            return;
        }
        slashCommandHandler = new SlashCommandHandler(this);
    }

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        
        Task createHooks = CreateHooks();

        if (settings.Token == null)
        {
            Console.WriteLine("ERROR: No token");
            return;
        }
        await client.LoginAsync(TokenType.Bot, settings.Token);
        await createHooks;
        await client.StartAsync();

        await Task.Delay(-1);
    }

    public async Task CreateHooks()
    {
        client.Log += LogAsync;
        client.Ready += Client_Ready;
        client.SlashCommandExecuted += slashCommandHandler.SlCommandHandler;
    }

    public async Task Client_Ready()
    {
        Task guildCommandsTask = LoadGuildCommands();
        await guildCommandsTask;
    }

    public async Task LoadGuildCommands()
    {
        var guild = client.GetGuild(settings.GuildID);
        await Ping.CreateCommand(guild);
        await Phase.CreateCommand(guild);
        await Setup.CreateCommand(guild);
    }

    public async Task AddGuild(Guild guild)
    {
        guilds.Add(guild.GuildID, guild);
    }

    public async Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException cmdException)
        {
            Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                + $" failed to execute in {cmdException.Context.Channel}.");
            Console.WriteLine(cmdException);
        }
        else
        {
            Console.WriteLine($"[General/{message.Severity}] {message}");
        }

        return;
    }
}

public class Bot
{

}