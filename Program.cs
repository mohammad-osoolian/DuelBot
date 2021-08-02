using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Awesome {
    class Program {
        static ITelegramBotClient botClient;
        public static Dictionary<long, List<User>> BattleFields = new Dictionary<long, List<User>>();
        // public static List<User> Fighters = new List<User>();
        public static bool InFight = false;
        public class Help
        {
            public static async Task SetAsarHelp(long chatid)
            {
                string helptext="Usage: setasar <owner> <asar>";
                await botClient.SendTextMessageAsync(chatid,helptext);
            }
            public static async Task GetAsarHelp(long chatid)
            {
                string helptext="Usage: getasar <owner> <spamnum> / getasar <owner>";
                await botClient.SendTextMessageAsync(chatid,helptext);
            }
        }
        static async void Bot_OnMessage(object sender, MessageEventArgs e) 
        {
            if (e.Message.Text == null)
                return;
            Message message = e.Message;
            switch (message.Text)
            {
                case "/help":
                    await help(message);
                    break;
                case "مایل به دوئل":
                    await AddFighter(message);
                    break;
            }
        }
        public static async Task help(Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "this bot is just a test bot");
        }
        public static async Task AddFighter(Message message)
        {
            CheckBattleField(message);
            long chatid = message.Chat.Id;
            List<User> fighters = BattleFields[chatid];
            User Fighter = message.From;
            if (fighters.Contains(Fighter))
            {
                await botClient.SendTextMessageAsync(chatid,"هستی", replyToMessageId:message.MessageId);
                return;
            }
            if (fighters.Count >= 6)
            {
                await botClient.SendTextMessageAsync(chatid,"دیر اومدی پره.");
                return;
            }
            if (InFight)
            {
                fighters.Add(message.From);
                await botClient.SendTextMessageAsync(chatid, $"{Fighter.Username} \n به دوئل اضافه شد.");
            }
            else
            {
                Task task = Task.Delay(30000).ContinueWith(KillByDice, message.Chat.Id);
                InFight = true;
                await botClient.SendTextMessageAsync(chatid, $"دوئل شروع شد.");
                await botClient.SendTextMessageAsync(chatid, $"شصت ثانیه وقت دارین تمایلتون نسبت به شرکت در دوئل رو اعلام کنین.");
                await botClient.SendTextMessageAsync(chatid, $"از الان حرف آخرتونو آماده کنین.");
                fighters.Add(message.From);
            }
        }
        public static async void KillByDice(Task task, object o)
        {
            long chatid = (long) o;
            var fighters = BattleFields[chatid];
            if(fighters.Count == 1)
            {
                await botClient.SendTextMessageAsync(chatid,$"بمولا مرد فقط خودتی. اینجا کسی جرعت نداره با تو دوئل کنه \n {fighters[0].Username}");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatid, MakekillBoard(chatid));
                int num = await SendDice(chatid, Enumerable.Range(1,fighters.Count).ToArray());
                User LoserFighter = fighters[num-1];
                await botClient.SendTextMessageAsync(chatid, $"پنج ثانیه وقت داری آخرین حرفتو بفرستی. \n {LoserFighter.Username}");
                Task kicktask = Task.Delay(5000).ContinueWith((task)=>{KickFighter(chatid, LoserFighter);});
            }
            BattleFields[chatid] = new List<User>();
            InFight = false;
        }
        public static async void KickFighter(long chatid, User fighter)
        {
            // long chatid = (long)ochatid;
            // User fighter = ofighter as User;
            try
            {
                await botClient.KickChatMemberAsync(chatid, fighter.Id);
                await botClient.SendTextMessageAsync(chatid, $"شوخوش.");
                await botClient.UnbanChatMemberAsync(chatid, fighter.Id);
            }
            catch(Telegram.Bot.Exceptions.ApiRequestException apire) when(apire.Message.Contains("can't remove chat owner"))
            {
                await botClient.SendTextMessageAsync(chatid,"اونر حرمت داره. ما حرمت شکنی نمیکنیم.");
                await botClient.SendTextMessageAsync(chatid, $"شوخوش.");
            }
            catch(Telegram.Bot.Exceptions.ApiRequestException apire) when(apire.Message.Contains("user is an administrator of the chat"))
            {
                await botClient.SendTextMessageAsync(chatid,"تاس حرمت داره و حرمتشم واجب، ولی متاسفانه گودرتشو ندارم ادمینو کیک کنم.");
                await botClient.SendTextMessageAsync(chatid, $"شوخوش.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static async Task<int> SendDice(long chatid, params int[] choises)
        {
            Message m;
            while (true)
            {
                m = await botClient.SendDiceAsync(chatid);
                Task.Delay(4000).Wait();
                if (choises.Contains(m.Dice.Value))
                    break;
            }
            return m.Dice.Value;
        }
        public static void CheckBattleField(Message message)
        {
            if (!BattleFields.ContainsKey(message.Chat.Id))
                BattleFields[message.Chat.Id] = new List<User>();
        }
        public static string MakekillBoard(long chatid)
        {
            var fighters = BattleFields[chatid];
            string result = string.Empty;
            for (int i = 0; i<fighters.Count(); i++)
                result += $"{i+1} ==> {fighters[i].Username}\n";
            return result;
        }
        static void Main()
        {
            string initjsondata = JsonConvert.SerializeObject(new Dictionary<int, Dictionary<string, string>>());
            System.IO.File.WriteAllText("Aasaar.txt", initjsondata);
            botClient = new TelegramBotClient("1793458084:AAGANlJP0GM9LChk2B3FiRTY7XXsLfAIJ1U");
            var me = botClient.GetMeAsync().Result;
            Console.WriteLine(
                $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );

            botClient.OnMessage += Bot_OnMessage;
            botClient.OnMessageEdited += Bot_OnMessage;
            botClient.StartReceiving();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            botClient.StopReceiving();
            
        }
    }
}

