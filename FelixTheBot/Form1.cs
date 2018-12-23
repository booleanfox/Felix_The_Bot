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

namespace FelixTheBot
{
    public partial class Form1 : Form
    {

       
       
        Telegram.Bot.TelegramBotClient Botik;
        string token;
        List<ProxyInfo> proxyInfos;
        HttpToSocks5Proxy proxy;
        AIMLbot.Bot brain;

        public Form1()
        {
            InitializeComponent();
            var lines = File.ReadAllLines("credentials.txt");

            token = lines[0];
            foreach (string s in lines.Skip(1))
            {
                var t = s.Split(':');
                if (t.Length >= 2 && int.TryParse(t[1], out int port)) {
                    proxyInfos.Add(new ProxyInfo(t[0], port));
                }
            }
            proxy = new HttpToSocks5Proxy(proxyInfos.ToArray());
            proxy.ResolveHostnamesLocally = true;
            if (!proxy.IsBypassed(new Uri("https://telegram.org/")))
            {
                throw new Exception("No proxies work!");

            }

            InitBrain();
            InitBot();
        }


        void InitBrain()
        {
            brain = new Bot();
            brain.loadSettings(Path.Combine(Environment.CurrentDirectory, Path.Combine("config", "Settings.xml")));
            brain.loadAIMLFromFiles();
            brain.isAcceptingUserInput = true;
            
        }

        async void InitBot()
        {
            Botik = new Telegram.Bot.TelegramBotClient(token, proxy);
            if(  ! await Botik.TestApiAsync())
            {
                throw new Exception("Can't reach Telegram");
            }
            Botik.OnMessage += Botik_OnMessage;
            Botik.StartReceiving();
        }

        private void Botik_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            
        }

        async void bw_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                string settingsPath;
                Bot botik_aiml = new Bot();
               // botik_aiml.loadSettings(settingsPath);
               // User myUser = new User();
                botik_aiml.isAcceptingUserInput = false;
                botik_aiml.loadAIMLFromFiles();
                botik_aiml.isAcceptingUserInput = true;

                int offset = 0; // message indent
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

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }
    }
}
