using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using AIMLbot;
using MihaZupan;
using System.Net.NetworkInformation;
namespace FelixTheBot
{
    public partial class Form1 : Form
    {

        private readonly object UsersDBlock = new object();

        Telegram.Bot.TelegramBotClient Botik;
        string token;
        List<ProxyInfo> proxyInfos;
        HttpToSocks5Proxy proxy;
        Bot brain;
        Telegram.Bot.Types.User Botik_identity;
        int offset = 0;
        bool is_silent = false;

        Dictionary<int,User> users= new Dictionary<int, User>();

       

        private User GetUser(Telegram.Bot.Types.User u)
        {
            int id = u.Id;
            lock (UsersDBlock)
            {
                if (!users.ContainsKey(id))
                {                 
                    users.Add(id, new AIMLbot.User(u.FirstName + " " + u.LastName, brain));
                }
                return users[id];
            }

        }


        public Form1()
        {
            InitializeComponent();
            var lines = File.ReadAllLines("credentials.txt");

            token = lines[0];
            proxyInfos = new List<ProxyInfo>();
            foreach (string s in lines.Skip(1))
            {
                 
                var t = s.Split( new char[] { ':' },StringSplitOptions.RemoveEmptyEntries);
                if (t.Length >= 2 && int.TryParse(t[1], out int port)) {
                    string addr = t[0]; 
                    if (t.Length >= 4)
                    {
                        string login = t[2];
                        string pass = t[3];

                        proxyInfos.Add(new ProxyInfo(addr, port, login, pass));
;                    }
                    else
                    {

                        proxyInfos.Add(new ProxyInfo(addr, port));

                    }
                }

            }

            
            GetBotTestProxies();
            InitBot();
            InitBrain();

          

        }

        void GetBotTestProxies()
        {
            foreach (var prox in proxyInfos)
            {

                proxy = new HttpToSocks5Proxy(new ProxyInfo[] { prox })
                {
                    //ResolveHostnamesLocally = true
                };
                
                try
                {
                    Botik = new Telegram.Bot.TelegramBotClient(token, proxy);
                    Botik_identity = Botik.GetMeAsync().Result;
                    if (Botik_identity != null)
                        return;
                }
                catch (Exception)
                {

                    
                }
                   
            }
            
            throw new Exception("No proxies work");


        }


        void InitBrain()
        {
            brain = new Bot();
            string settingsPath = Path.Combine(Environment.CurrentDirectory, Path.Combine("config", "Settings.xml"));
            brain.loadSettings(settingsPath);
            brain.isAcceptingUserInput = false;
            brain.loadAIMLFromFiles();
            brain.isAcceptingUserInput = true;
       
          
        }

         void InitBot()
        {
           
            
           

           
            
            
            

        }

        private void Botik_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            
        }

        private void Botik_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            var command  = message.Text.Trim();
            if (command == $"SILENCE @{Botik_identity.Username}" || command == "SILENCE_ALL")
                is_silent = true;
            else if (command == $"UNSILENCE @{Botik_identity.Username}" || command == "UNSILENCE_ALL")
                is_silent = false;
            else if (command == $"REPORT @{Botik_identity.Username}" || command == "REPORT_ALL")
            {
                Botik.SendTextMessageAsync(message.Chat.Id,  $"Я {Botik_identity.Username}, ставьте лайк и подписывайтесь!", replyToMessageId: message.MessageId);
            }
            else if (!is_silent
                && message.Type == Telegram.Bot.Types.Enums.MessageType.Text
                && message.From.Id != Botik_identity.Id
                && message.Text.Contains("@"+Botik_identity.Username))
            {

                User user = GetUser(message.From);
                string text = message.Text;
                var res = brain.Chat(text, user.UserID);
                string reply = res.RawOutput.Trim();
                if (reply == "")
                    return;
                Botik.SendTextMessageAsync(message.Chat.Id, reply, replyToMessageId: message.MessageId);

            }

        }

        async void bw_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                string settingsPath;
                Bot botik_aiml = new Bot
                {
                    // botik_aiml.loadSettings(settingsPath);
                    // User myUser = new User();
                    isAcceptingUserInput = false
                };
                botik_aiml.loadAIMLFromFiles();
                botik_aiml.isAcceptingUserInput = true;

                // message indent
                while (true)
                {
                    var updates = await Botik.GetUpdatesAsync(offset);

                    foreach (var update in updates)
                    {
                        var message = update.Message;
                        if (message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                        {
                            if (message.Text == "/saysomething")
                            {
                                await Botik.SendTextMessageAsync(message.Chat.Id, "hey hows it going bros!",
                                       replyToMessageId: message.MessageId);
                            }

                            //Request r = new Request(message.Text, myUser, botik_aiml);
                            //Result res = botik_aiml.Chat(r);

                            //await Botik.SendTextMessageAsync(message.Chat.Id, res.Output);
                        }
                        offset = update.Id + 1;
                    }

                }

            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                Console.WriteLine(ex.Message); // if wrong token
            }
        }

        void buttonStart_Click(object sender, EventArgs e)
        {
            if (Botik.IsReceiving)
            {
                Botik.StopReceiving();
                Botik.OnMessage -= Botik_OnMessage;
                label1.ForeColor = Color.Red;
                label1.Text = "Бот отключен";
                buttonStart.Text = "Включить бота!";
            }
            else
            {
                Botik.StartReceiving();
                Botik.OnMessage += Botik_OnMessage;
                label1.ForeColor = Color.Green;
                label1.Text = "Бот включен";
                buttonStart.Text = "Выключить бота!";
            }


        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
