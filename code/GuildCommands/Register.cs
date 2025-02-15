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
            command.AddOption("user", ApplicationCommandOptionType.User, "User", isRequired: true);
            command.AddOption("name", ApplicationCommandOptionType.String, "The user's name.", isRequired: true);
            command.AddOption("channel", ApplicationCommandOptionType.Channel, "Player channel", isRequired: true);
            command.AddOption("multilink", ApplicationCommandOptionType.User, "If this is to register user to an existing player, write existing player here", isRequired: false);

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
                Player? userPlayer = guild.Players.Find(player => player.IsPlayer(userID));
                if (userPlayer != null) {
                    await command.RespondAsync($"Player {userPlayer.Name} already exists! Cannot register existing player.");
                    return;
                }

                ulong channelID = ((SocketGuildChannel)command.Data.Options.ElementAt(2).Value).Id;
                string name = (string)command.Data.Options.ElementAt(1).Value;
                if(command.Data.Options.Count > 3) {
                    
                    ulong mainID = ((SocketGuildUser)command.Data.Options.ElementAt(3).Value).Id;
                    Player? player = guild.Players.Find(player => player.IsPlayer(mainID));
                    if(player == null) {
                        await command.RespondAsync($"Could not find main player {program.client.GetUserAsync(mainID)} to link to, are they a registered player?");

                    } else {
                        player.LinkedIDs.Add(userID);
                        player.LinkedNames[userID] = name;
                        await command.RespondAsync($"Registered user {program.client.GetUserAsync(userID)} under {program.client.GetUserAsync(mainID)}.");
                    }
                } else {
                    guild.AddPlayer(new Player
                                   (userID, channelID, name));
                    await command.RespondAsync($"Registered new player {program.client.GetUserAsync(userID)} with channel {program.client.GetChannelAsync(channelID)}.");

                }
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