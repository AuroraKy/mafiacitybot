using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class Actions
    { 
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("action");
            command.WithDescription("Change your day/night action.");
            command.AddOption(new SlashCommandOptionBuilder()
                .WithName("view")
                .WithDescription("View your current Action")
                .WithType(ApplicationCommandOptionType.SubCommand)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("set")
                .WithDescription("Set your current Action")
                .WithType(ApplicationCommandOptionType.SubCommand)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("clear")
                .WithDescription("Clears your current Action")
                .WithType(ApplicationCommandOptionType.SubCommand)
            );



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
            var user = command.User;
            var channel = command.Channel;

            if (!program.guilds.TryGetValue((ulong)command.GuildId, out Guild guild))
            {
                await command.RespondAsync($"You must use setup before being able to use this command!");
                return;
            }

            if (guild.isLocked) {
                await command.RespondAsync($"Action commands are currently locked, please notify the hosts if you believe this is not intended!");
                return;
            }

            Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
            if (player == null || player.ChannelID != channel.Id)
            {
                await command.RespondAsync("This command can only be used by a player in their channel!");
                return;
            }

            var fieldName = command.Data.Options.First().Name;
            var options = command.Data.Options.First().Options;

            switch (fieldName)
            {
                case "view":
                    await command.RespondAsync(player.Action == "" ? "No action set." : player.Action);
                    break;
                case "set":
                    var mb = new ModalBuilder()
                        .WithTitle((guild.CurrentPhase == Guild.Phase.Day ? "Day Action" : "Night Action"))
                        .WithCustomId("action_modal")
                        .AddTextInput("Action", "action", TextInputStyle.Paragraph, maxLength: 2000, value:player.Action);

                    await command.RespondWithModalAsync(mb.Build());

                    // Modal is answered in program using a hook
                    break;
                case "clear":
                    player.Action = "";
                    await command.RespondAsync("Action cleared.");
                    guild.Save();
                    break;
            }

        }

        public static async Task ModalSubmitted(SocketModal modal)
        {

            if (modal.Data.CustomId != "action_modal") return;

            var user = modal.User;
            var channel = modal.Channel;

            if (!Program.instance.guilds.TryGetValue((ulong)modal.GuildId, out Guild guild))
            {
                await modal.RespondAsync($"You must use setup before being able to use this command!");
                return;
            }

            Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
            if (player == null || player.ChannelID != channel.Id)
            {
                await modal.RespondAsync("This command can only be used by a player in their channel!");
                return;
            }

            // Get the values of components.
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            string content = components.First(x => x.CustomId == "action").Value;

            // Build the message to send.
            player.Action = content;

            // Respond to the modal.
            await modal.RespondAsync("Set action:\n" + content.Substring(0, Math.Min(content.Length, 130)) + (content.Length < 130 ? "" : "..."));

            guild.Save();
        }
    }
}