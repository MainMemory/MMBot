using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using MMBot;

namespace MMBotQuiz
{
    public class QuizModule : BotModule
    {
        public QuizModule()
        {
            string[] q = Properties.Resources.QuizQuestions.SplitByString("\r\n");
            List<Question> questions = new List<Question>();
            for (int i = 0; i <= q.Length - 1; i += 4)
                questions.Add(new Question(q[i], q[i + 1], q[i + 2], q[i + 3]));
            Questions = new ReadOnlyCollection<Question>(questions);
            ChangeDirectory();
            if (File.Exists("Quiz.json"))
            {
                Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                StreamReader sr = new StreamReader("Quiz.json");
                Newtonsoft.Json.JsonTextReader jr = new Newtonsoft.Json.JsonTextReader(sr);
                Scores = js.Deserialize<Dictionary<string, Dictionary<string, int>>>(jr);
                jr.Close();
                sr.Close();
            }
            RestoreDirectory();
        }

        ReadOnlyCollection<Question> Questions;

        Dictionary<string, int> chanquestions = new Dictionary<string, int>();

        Dictionary<string, Dictionary<string, int>> Scores = new Dictionary<string, Dictionary<string, int>>();

        public override void Shutdown() { Save(); }

        public override void  Save()
        {
            ChangeDirectory();
            Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
            StreamWriter stw = new StreamWriter("Quiz.json");
            Newtonsoft.Json.JsonTextWriter jw = new Newtonsoft.Json.JsonTextWriter(stw);
            js.Serialize(jw, Scores);
            jw.Close();
            stw.Close();
            RestoreDirectory();
        }

        void QuizQuestionCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            int lastq = chanquestions.ContainsKey(channel) ? chanquestions[channel] : -1;
            int q = Module1.Random.Next(Questions.Count);
            while (q == lastq)
                q = Module1.Random.Next(Questions.Count);
            chanquestions[channel] = q;
            IrcObject.WriteMessage(Questions[q].Title, channel);
            IrcObject.WriteMessage(Questions[q].A, channel);
            IrcObject.WriteMessage(Questions[q].B, channel);
            IrcObject.WriteMessage(Questions[q].C, channel);
        }

        void QuizACommand(IRC IrcObject, string channel, string user, string command)
        {
            CheckQuizAnswer(IrcObject, channel, user, 1);
        }

        void QuizBCommand(IRC IrcObject, string channel, string user, string command)
        {
            CheckQuizAnswer(IrcObject, channel, user, 2);
        }

        void QuizCCommand(IRC IrcObject, string channel, string user, string command)
        {
            CheckQuizAnswer(IrcObject, channel, user, 3);
        }

        void CheckQuizAnswer(IRC IrcObject, string channel, string user, int answer)
        {
            if (chanquestions.ContainsKey(channel))
            {
                if (!Scores.ContainsKey(channel))
                    Scores.Add(channel, new Dictionary<string, int>());
                if (!Scores[channel].ContainsKey(user))
                    Scores[channel].Add(user, 0);
                if (answer == Questions[chanquestions[channel]].correct)
                {
                    Scores[channel][user]++;
                    IrcObject.WriteMessage(user + ": Yes, " + Module1.Choose(answer, "A", "B", "C") + " is the correct answer! +1 point. Current score: " + Scores[channel][user], channel);
                    chanquestions.Remove(channel);
                }
                else
                {
                    Scores[channel][user]--;
                    IrcObject.WriteMessage(user + ": No, " + Module1.Choose(answer, "A", "B", "C") + " is not the correct answer. -1 point. Current score: " + Scores[channel][user], channel);
                }
            }
            else
                IrcObject.WriteMessage("You need to get a question first!", channel);
        }

        void QuizScoreCommand(IRC IrcObject, string channel, string user, string command)
        {
            int score = 0;
            if (Scores.ContainsKey(channel) && Scores[channel].ContainsKey(user))
                score = Scores[channel][user];
            IrcObject.WriteMessage("Your current score is " + score + ".", channel);
        }

        void QuizTopCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!Scores.ContainsKey(channel) || Scores[channel].Count == 0)
            {
                IrcObject.WriteMessage("No scores have been counted for this channel.", channel);
                return;
            }
            int num = 5;
            if (!string.IsNullOrEmpty(command))
                num = int.Parse(command, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
            List<KeyValuePair<string, int>> scores = new List<KeyValuePair<string, int>>(Scores[channel]);
            scores.Sort((a, b) => -a.Value.CompareTo(b.Value));
            num = System.Math.Min(num, scores.Count);
            string message = "Top " + num + " scores:";
            for (int i = 0; i < num; i++)
                message += " " + Module1.UnderChar + scores[i].Key + ": " + scores[i].Value + Module1.UnderChar;
            IrcObject.WriteMessage(message, channel);
        }

        void QuizBottomCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!Scores.ContainsKey(channel) || Scores[channel].Count == 0)
            {
                IrcObject.WriteMessage("No scores have been counted for this channel.", channel);
                return;
            }
            int num = 5;
            if (!string.IsNullOrEmpty(command))
                num = int.Parse(command, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo);
            List<KeyValuePair<string, int>> scores = new List<KeyValuePair<string, int>>(Scores[channel]);
            scores.Sort((a, b) => a.Value.CompareTo(b.Value));
            num = System.Math.Min(num, scores.Count);
            string message = "Bottom " + num + " scores:";
            for (int i = 0; i < num; i++)
                message += " " + Module1.UnderChar + scores[i].Key + ": " + scores[i].Value + Module1.UnderChar;
            IrcObject.WriteMessage(message, channel);
        }
    }

    internal class Question
    {
        public string Title;
        public string A;
        public string B;
        public string C;

        public byte correct;
        public Question(string q, string a1, string a2, string a3)
        {
            Title = q;
            if (a1.StartsWith(">"))
            {
                a1 = a1.Substring(1);
                correct = 1;
            }
            A = a1;
            if (a2.StartsWith(">"))
            {
                a2 = a2.Substring(1);
                correct = 2;
            }
            B = a2;
            if (a3.StartsWith(">"))
            {
                a3 = a3.Substring(1);
                correct = 3;
            }
            C = a3;
        }
    }
}