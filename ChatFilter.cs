using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using TShockAPI;
using ProfanityFilter;
using TShockAPI.Hooks;

namespace ChatFilter
{
    public class ChatFilter : TerrariaPlugin
    {
        #region Plugin info
        public override string Name => "ChatFilter";

        public override Version Version => new Version(1, 0);

        public override string Author => "nyan-ko";
        #endregion

        private ProfanityFilter.ProfanityFilter _filter;
        private ProfanityDictionary _dict;

        public ChatFilter(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            PlayerHooks.PlayerChat += OnChat;

            _dict = ProfanityDictionary.Read();
            _filter = new ProfanityFilter.ProfanityFilter(_dict.BannedWords);

            Commands.ChatCommands.Add(new Command("chatfilter.admin", Profanity, "chatfilter", "cf"));
            Commands.ChatCommands.Add(new Command("chatfilter.admin", ToggleRegisteredUserCheck, "cfcheck"));
        }

        private void OnChat(PlayerChatEventArgs args)
        {
            var plr = args.Player;

            TShock.Utils.Broadcast(plr.Group?.Name ?? "null", Color.Red);

            if ((plr.Group?.Name ?? "") != "" && !_dict.CheckRegistered)
            {
                return;
            }

            args.RawText = _filter.CensorString(args.RawText);
        }

        private void Profanity(CommandArgs args)
        {
            var sub = args.Parameters.Count == 0 ? "help" : args.Parameters[0];
            switch (sub)
            {
                default:
                case "help":
                {
                    var help = new List<string>()
                    {
                        "Command for managing the chat filter.",
                        "/cf add - Adds a word to the profanity list.",
                        "/cf del - Deletes a word from the profanity list.",
                        "/cf list - Lists the contents of the profanity list.",
                        "/cfcheck - Toggles filtering messages from registered users."
                    };

                    PaginationTools.SendPage(args.Player, 0, help);
                }
                    break;
                case "add":
                {
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("Expected a word to add.");
                        return;
                    }

                    string badWord = string.Join(" ", args.Parameters.Skip(1));

                    AddNewProfanity(badWord);

                    args.Player.SendSuccessMessage($"Successfully added {badWord} as profanity.");
                }
                    break;
                case "delete":
                case "del":
                {
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("Expected a word to remove.");
                        return;
                    }

                    string badWord = string.Join(" ", args.Parameters.Skip(1));

                    bool success = _dict.BannedWords.Remove(badWord) | _filter.RemoveProfanity(badWord);
                    _dict.Write();

                    if (success)
                    {
                        args.Player.SendSuccessMessage($"Successfully removed {badWord} as profanity.");
                    }
                    else
                    {
                        args.Player.SendSuccessMessage($"Could not find {badWord} to remove.");
                    }
                }
                    break;
                case "list":
                {
                    if (PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out int pg))
                    {
                        return;
                    }

                    PaginationTools.SendPage(args.Player, pg, _dict.BannedWords,
                       new PaginationTools.Settings
                       {
                           HeaderFormat = "Profanity List ({0}/{1})",
                           FooterFormat = "Type /cf list {0} for more.",
                           NothingToDisplayString = "There are currently no bad words."
                       });
                    break;
                }
            }

        }

        private void ToggleRegisteredUserCheck(CommandArgs args)
        {
            _dict.CheckRegistered = !_dict.CheckRegistered;
            args.Player.SendSuccessMessage($"{(_dict.CheckRegistered ? "En" : "Dis")}abled checking registered users for profanity.");
        }

        private void AddNewProfanity(string badWord)
        {
            _dict.BannedWords.Add(badWord);
            _filter.AddProfanity(badWord);
            _dict.Write();
        }
    }
}
