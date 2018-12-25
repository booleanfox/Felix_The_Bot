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
        private readonly object TextBoxLock = new object();
        Telegram.Bot.TelegramBotClient Botik;
        string token;
        List<ProxyInfo> proxyInfos;
        HttpToSocks5Proxy proxy;
        Bot brain;
        Telegram.Bot.Types.User Botik_identity;
        int offset = 0;
        bool is_silent = false;
        bool is_blaber = false;
        Dictionary<int,User> users= new Dictionary<int, User>();

        public delegate void message(string text);
        public event message NewText;

        private void getEvent(string text)
        {
            richTextBox1.Text += text;

        }



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
            NewText += getEvent;
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
            brain.TrustAIML = true;
           
        }

         void InitBot()
        {
          
        }

        public void AddText(string text)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                Action<string> updaterdelegate = new Action<string>(AddText);
                try
                {
                    this.Invoke(updaterdelegate, new object[] { text });
                }
                catch (ObjectDisposedException ex) { }
            }
            else
            {
                richTextBox1.Text += text;
            }
        }

        private void Botik_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            
        }

        private void Botik_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return;
            AddText($"{message.From.Username}: {message.Text} \n");
            var command = message.Text.Trim();
            if (command == $"SILENCE @{Botik_identity.Username}" || command == "SILENCE_ALL")
            {
                is_silent = true;
                AddText("SILENCE MODE ON\n");
            }
            else if (command == $"UNSILENCE @{Botik_identity.Username}" || command == "UNSILENCE_ALL")
            {
                is_silent = false;
                AddText("SILENCE MODE OFF\n");
            }
            else if (command == $"BLABER_ON @{Botik_identity.Username}" || command == "BLABER_ON_ALL")
            {
                is_blaber = true;
                AddText("BLABER MODE ON\n");
            }
            else if (command == $"BLABER_OFF @{Botik_identity.Username}" || command == "BLABER_OFF_ALL")
            {
                is_blaber = false;
                AddText("BLABER MODE OFF\n");
            }
            else if (is_silent)
                return;
            else if (command == $"REPORT @{Botik_identity.Username}" || command == "REPORT_ALL")
            {
                string reply = $"{Botik_identity.Username}: Я {Botik_identity.Username}, ставьте лайк и подписывайтесь!";
                Botik.SendTextMessageAsync(message.Chat.Id, reply, replyToMessageId: message.MessageId);
                AddText(reply+"\n");
            }
            else if (
                        message.From.Id != Botik_identity.Id
                        && 
                        (
                             message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private ||
                             is_blaber ||
                             message.Text.Contains("@" + Botik_identity.Username)
                        )
                    )
            {
                User user = GetUser(message.From);
                Request request = new Request(message.Text, user, brain);
                Result result = brain.Chat(request);
               

                string reply = result.Output; ;

                if (reply == null)
                    throw new Exception("null output");                                
                Botik.SendTextMessageAsync(message.Chat.Id, reply, replyToMessageId: message.MessageId);
                AddText($"{Botik_identity.Username} to {message.From.Username}: {reply} \n");
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
