using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.Json;
using mafiacitybot.GuildCommands;
using System.Reflection;
using System.Diagnostics;
using System;

namespace mafiacitybot;

public class Program
{
    public DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig() {
        AlwaysDownloadUsers = true,
        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.AllUnprivileged - GatewayIntents.GuildScheduledEvents - GatewayIntents.GuildInvites
    });
    public Settings settings;
    public Dictionary<ulong, Guild> guilds = new Dictionary<ulong, Guild>();
    public SlashCommandHandler slashCommandHandler;
    public static Program instance;
    public static string DataPath = ( false ? "../../../../Data" : "./Data"); //set to false when putting on raspberry pi
    public static string VERSION = "1.2.0";

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

    public async Task CreateCommands() {
        _ = LogAsync(new LogMessage(LogSeverity.Info, "CreateCommands", "Creating commands..."));

        
        foreach (SocketGuild guild in client.Guilds) {
            await Ping.CreateCommand(client, guild);
            await Phase.CreateCommand(client, guild);
            await Setup.CreateCommand(client, guild);
            await Register.CreateCommand(client, guild);
            await Letter.CreateCommand(client, guild);
            await HostLetter.CreateCommand(client, guild);
            await Actions.CreateCommand(client, guild);
            await ClearPlayers.CreateCommand(client, guild);
            await ClearPlayer.CreateCommand(client, guild);
            await Info.CreateCommand(client, guild);
            await EnforceRoles.CreateCommand(client, guild);
            await ViewActions.CreateCommand(client, guild);
            await ViewAction.CreateCommand(client, guild);
            await Help.CreateCommand(client, guild);
            await HostLetter.CreateCommand(client, guild);
            await Lock.CreateCommand(client, guild);
            if (guild.Id == 1167188182262095952u)
            {
                await RegisterCommands.CreateCommand(client, guild);
            }
            _ = LogAsync(new LogMessage(LogSeverity.Info, "CreateCommands", "Done creating commands for guild "+guild.Name+" ("+guild.Id+")"));
        }
        _ = LogAsync(new LogMessage(LogSeverity.Info, "CreateCommands", "Done creating commands..."));
    }
    public async Task LoadGuildCommands()
    {

        foreach (SocketGuild guild in client.Guilds)
        {
            Guild? g = Guild.Load(guild.Id);
            if (g != null) await AddGuild(g);
        }

        client.ModalSubmitted += Letter.ModalSubmitted;
        client.ModalSubmitted += HostLetter.ModalSubmitted;
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