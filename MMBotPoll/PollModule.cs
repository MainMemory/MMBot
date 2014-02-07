using System;
using System.Collections.Generic;
using MMBot;
using System.Collections.ObjectModel;

namespace MMBotPoll
{
    public class PollModule : BotModule
    {
        public PollModule() { }
        public override void Shutdown() { }

        Dictionary<string, PollInfo> Polls = new Dictionary<string, PollInfo>();

        void PollStartCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (channel.StartsWith("#"))
            {
                if (Polls.ContainsKey(channel.ToLowerInvariant()))
                {
                    IrcObject.WriteMessage("There is already a poll running on this channel!", channel);
                    return;
                }
                List<string> pollans = new List<string>(Module1.ParseCommandLine(command));
                string pollq = pollans[0];
                pollans.RemoveAt(0);
                Polls.Add(channel.ToLowerInvariant(), new PollInfo(pollq, pollans, user));
                IrcObject.WriteMessage(user + " has started a poll: " + pollq, channel);
                string message = "Responses:";
                for (int j = 0; j < pollans.Count; j++)
                    message += " " + Module1.UnderChar + (j + 1) + ": \"" + pollans[j] + "\"" + Module1.UnderChar;
                IrcObject.WriteMessage(message, channel);
                IrcObject.WriteMessage("Vote with /msg " + IrcObject.IrcNick + " poll vote " + channel + " " + Module1.UnderChar + "number" + Module1.UnderChar + ".", channel);
            }
            else
                IrcObject.WriteMessage("You must start a poll from a channel!", channel);
        }

        void PollProgressCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (channel.StartsWith("#"))
            {
                if (!Polls.ContainsKey(channel.ToLowerInvariant()))
                {
                    IrcObject.WriteMessage("There is no poll running on this channel!", channel);
                    return;
                }
                PollInfo info = Polls[channel.ToLowerInvariant()];
                IrcObject.WriteMessage(info.Responses.Count + " " + (info.Responses.Count == 1 ? "person has" : "people have") + " responded to " + info.Starter + "'s poll: " + info.Question, channel);
            }
            else
                IrcObject.WriteMessage("This command must be used from a channel!", channel);
        }

        void PollEndCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (channel.StartsWith("#"))
            {
                if (!Polls.ContainsKey(channel.ToLowerInvariant()))
                {
                    IrcObject.WriteMessage("There is no poll running on this channel!", channel);
                    return;
                }
                PollInfo info = Polls[channel.ToLowerInvariant()];
                if (user != info.Starter)
                {
                    IrcObject.WriteMessage("Polls can only be ended by the person that started them!", channel);
                    return;
                }
                IrcObject.WriteMessage(user + " has ended the poll: " + info.Question, channel);
                Dictionary<int,int> responses = new Dictionary<int,int>();
                for (int i = 0; i < info.Answers.Count; i++)
                    responses.Add(i, 0);
                foreach (KeyValuePair<string, int> answer in info.Responses)
                    responses[answer.Value]++;
                List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>(responses);
                list.Sort((a, b) => -a.Value.CompareTo(b.Value));
                string message = "Top " + Math.Min(list.Count, 5) + " response" + (list.Count == 1 ? "" : "s") + ":";
                for (int i = 0; i < Math.Min(list.Count, 5); i++)
                    message += " " + Module1.UnderChar + '"' + info.Answers[list[i].Key] + "\" " + list[i].Value + " (" + Math.Round(list[i].Value / (double)info.Responses.Count, 3, MidpointRounding.AwayFromZero) * 100 + "%)" + Module1.UnderChar;
                IrcObject.WriteMessage(message, channel);
                Polls.Remove(channel.ToLowerInvariant());
            }
            else
                IrcObject.WriteMessage("This command must be used from a channel!", channel);
        }

        void PollVoteCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#"))
            {
                string[] split = command.Split(' ');
                if (!Polls.ContainsKey(split[0].ToLowerInvariant()))
                {
                    IrcObject.WriteMessage("There is no poll running on that channel!", channel);
                    return;
                }
                PollInfo info = Polls[split[0].ToLowerInvariant()];
                if (user == info.Starter)
                {
                    IrcObject.WriteMessage("You can't vote in your own poll!", channel);
                    return;
                }
                if (info.Responses.ContainsKey(user))
                {
                    IrcObject.WriteMessage("You can only vote once per poll!", channel);
                    return;
                }
                int ans = int.Parse(split[1], System.Globalization.NumberStyles.Number, System.Globalization.NumberFormatInfo.InvariantInfo) - 1;
                if (ans > -1 & ans < info.Answers.Count)
                {
                    info.Responses.Add(user, ans);
                    IrcObject.WriteMessage("You have voted for \"" + info.Answers[ans] + "\".", channel);
                }
                else
                    IrcObject.WriteMessage("Invalid option!", channel);
            }
            else
                IrcObject.WriteMessage("This command cannot be used from a channel!", channel);
        }
    }

    internal class PollInfo
    {
        public string Question { get; private set; }
        public ReadOnlyCollection<string> Answers { get; private set; }
        public string Starter { get; private set; }
        public Dictionary<string, int> Responses { get; private set; }

        public PollInfo(string question, IList<string> answers, string starter)
        {
            Question = question;
            Answers = new ReadOnlyCollection<string>(answers);
            Starter = starter;
            Responses = new Dictionary<string, int>();
        }
    }
}