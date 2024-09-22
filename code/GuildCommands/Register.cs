using Discord;
using Discord.Net;
using Discord.WebSocket;
using System;

namespace mafiacitybot.GuildCommands
{
    public static class Register
    {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithDefaultMemberPermissions(GuildPermission.ManageRoles);
            command.WithName("register");
            command.WithDescription("Registers a player and their personal channel.");
            command.AddOption("user", ApplicationCommandOptionType.User, "change this later", isRequired: true);
            command.AddOption("name", ApplicationCommandOptionType.String, "The user's name.", isRequired: true);
            command.AddOption("channel", ApplicationCommandOptionType.Channel, "change this later", isRequired: true);

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
            try
            {
                if(!program.guilds.TryGetValue(Convert.ToUInt64(command.GuildId), out Guild guild))
                {
                    await command.RespondAsync("You must use Setup first!");
                    return;
                }
                if (!Guild.IsHostRoleUser(command, guild.HostRoleID)) {
                    await command.RespondAsync($"You must have the host role to use this command!");
                    return;
                }
                if (guild.HostChannelID != command.ChannelId) {
                    await command.RespondAsync($"Command must be executed in the host channel!");
                    return;
                }

                ulong userID = ((SocketGuildUser)command.Data.Options.ElementAt(0).Value).Id;
                ulong channelID = ((SocketGuildChannel)command.Data.Options.ElementAt(2).Value).Id;
                string name = (string)command.Data.Options.ElementAt(1).Value;
                guild.AddPlayer(new Player
                (userID, channelID, name));
                await command.RespondAsync($"Registered new player {program.client.GetUserAsync(userID)} with channel {program.client.GetChannelAsync(channelID)}.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                await command.RespondAsync("Something went wrong!");
            }
            
        }
    }
}