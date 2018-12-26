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
using System.Timers;

namespace FelixTheBot
{
    public partial class Form1 : Form
    {
        Telegram.Bot.Types.Chat last_chat;
        private readonly object UsersDBlock = new object();
        private readonly object TextBoxLock = new object();
        Telegram.Bot.TelegramBotClient Botik;
        string token;
        List<ProxyInfo> proxyInfos;
        HttpToSocks5Proxy proxy;
        Bot brain;
        Telegram.Bot.Types.User Botik_identity;
        int offset = 0;
        bool is_silent = true;
        bool is_blaber = false;
        Dictionary<int,User> users= new Dictionary<int, User>();
        System.Timers.Timer timer1;
        int timeout = 120000; // 120 seconds

        string cat_path = @"C:\Users\boole\Desktop\ФЕЛИКС\Felix_The_Bot\FelixTheBot\cat.jpg";

        public delegate void message(string text);
        public event message NewText;



        string[] memes = new string[]
        {
            @"https://pp.userapi.com/c850324/v850324542/9f583/I5KPC2Q1roc.jpg",
            @"https://pp.userapi.com/c850236/v850236198/99ad4/4RZ-Pi8ReL4.jpg",
            @"https://pp.userapi.com/c846121/v846121936/15558a/8tbnqVF6xWE.jpg",
            @"https://pp.userapi.com/c850428/v850428949/771d6/mSjJpoWL7uU.jpg",
            @"https://pp.userapi.com/c844521/v844521644/15db57/YvHox2G16_k.jpg",
            @"https://sun1-8.userapi.com/c543108/v543108125/44234/i-8cutJNLD8.jpg",
            @"https://pp.userapi.com/c850324/v850324542/9f583/I5KPC2Q1roc.jpg",
            @"https://sun1-3.userapi.com/c543108/v543108765/38b7e/RwLHnJbGBPA.jpg"
        };


        Random r = new Random();

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
            timer1 = new System.Timers.Timer();
            timer1.Interval = timeout;
            timer1.Elapsed += Timer1_Elapsed;
            timer1.AutoReset = true;
            
        }

        private void Timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            if (is_silent || last_chat == null)
                return;
            Result result = brain.Chat("SILENCEINCHAT", "me");
            string reply = result.Output;

            if (reply == null)
                throw new Exception("null output");
            Botik.SendTextMessageAsync(last_chat, reply); // 
            AddText($"{Botik_identity.Username}: {reply} \n");
            
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

        

        private void Botik_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            timer1.Stop();
           
           
            var message = e.Message;
            last_chat = message.Chat;
            
       
            AddText($"{message.From.Username}: {message.Text} \n");
            var command = message.Text == null ? "" :message.Text.Trim();
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
                string reply = $"Я Пьюдипай, ставьте лайк и подписывайтесь!";
                Botik.SendTextMessageAsync(message.Chat.Id, reply, replyToMessageId: message.MessageId);
                AddText(reply+"\n");
            }
            else if ( message.From.Id != Botik_identity.Id)
            {

                User user = GetUser(message.From);
                string toAIML = null;
                bool Answer = message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private ||
                              is_blaber ||
                             (message.Text != null && message.Text.Contains("@" + Botik_identity.Username)) ||
                             (message.ReplyToMessage != null && message.ReplyToMessage.From.Id == Botik_identity.Id);

                switch (message.Type)
                {
            
                    case Telegram.Bot.Types.Enums.MessageType.Text:
                        toAIML = message.Text;
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.Photo:
                        toAIML = "GOTPHOTO";
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.Audio:
                        toAIML = "GOTAUDIO";
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.Video:
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.Voice:
                        toAIML = "GOTVOICE";
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.Document:
                        toAIML = "GOTDOCUMENT";
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.Sticker:
                        toAIML = "GOTSTICKER";
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.VideoNote:
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.ChatMembersAdded:
                        if (message.NewChatMembers.Count((Telegram.Bot.Types.User u) => u.Id == Botik_identity.Id) > 0)
                            toAIML = "FELIXJOINED";
                        else
                            toAIML = "NEWMEMBER " + string.Join(",",message.NewChatMembers.Select(u => u.Username));
                        Answer = true;  
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.ChatMemberLeft:
                        toAIML = "MEMBERLEFT " + message.LeftChatMember.Username;
                        Answer = true;
                        break;
                    case Telegram.Bot.Types.Enums.MessageType.MessagePinned:
                        break;
                    
                    default:
                        break;
                }

                if (toAIML != null && Answer)
                {
                    Request request = new Request(toAIML, user, brain);
                    Result result = brain.Chat(request);            
                    WorkResponce(result, message);

                }

               
                
            }

            timer1.Start();

        }

    
        private void WorkResponce(Result aiml_res, Telegram.Bot.Types.Message message)
        {
            string responce = aiml_res.Output.Trim();
            var chatid = message.Chat.Id;
            var replyid = message.MessageId;

            switch (responce)
            {
                case "SEND_CAT.":
                    using (var stream = File.Open(cat_path, FileMode.Open))
                    {
                        var rep = Botik.SendPhotoAsync(message.Chat.Id, stream, "", replyToMessageId: replyid).Result;
                    }                  
                    break;
                case "SEND_MEME.":
                    if(memes.Length > 0 )
                    {
                        var ind = r.Next() % memes.Length;
                        Botik.SendPhotoAsync(chatid, photo: memes[ind]);

                    }
                    break;
                default:
                    Botik.SendTextMessageAsync(chatid, responce, replyToMessageId: replyid);
                    break;
            }


            AddText($"{Botik_identity.Username} to {message.From.Username}: {responce} \n");

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
                timer1.Stop();
            }
            else
            {
                Botik.StartReceiving();

              
                timer1.Start();
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            is_blaber = checkBox1.Checked;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            is_silent = checkBox2.Checked;
        }

        
    }
}
