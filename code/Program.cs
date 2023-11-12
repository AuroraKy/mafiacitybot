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
    public static Program instance;
    //public static string DataPath = "./Data"; // for raspberry pi
    public static string DataPath = "../../../../Data"; // testing

    public Program()
    {
        instance = this;
        try
        {
            string text = File.ReadAllText(DataPath + "/settings.json");
            settings = JsonSerializer.Deserialize<Settings>(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to read settings.json: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
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
        foreach(SocketGuild guild in client.Guilds)
        {
            Guild? g = Guild.Load(guild.Id);
            if (g != null) await AddGuild(g);

            await Ping.CreateCommand(guild);
            await Phase.CreateCommand(guild);
            await Setup.CreateCommand(guild);
            await Register.CreateCommand(guild);
            await Letter.CreateCommand(guild);
            await Actions.CreateCommand(guild);
            await ClearPlayers.CreateCommand(guild);
        }

        client.ModalSubmitted += Letter.ModalSubmitted;
        client.ModalSubmitted += Actions.ModalSubmitted;
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