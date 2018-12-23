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

namespace FelixTheBot
{
    public partial class Form1 : Form
    {
        BackgroundWorker bw;
        Telegram.Bot.TelegramBotClient Botik;



        public Form1()
        {
            InitializeComponent();


            this.bw = new BackgroundWorker();
            this.bw.DoWork += this.bw_DoWork;
        }

        async void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;


            var key = e.Argument as String; // get bot token

            System.Net.WebProxy wp = new System.Net.WebProxy("54.39.144.247:9090", true);
            wp.Credentials = new System.Net.NetworkCredential("user1", "user1Password");
            Botik = new Telegram.Bot.TelegramBotClient(key, wp); // initialize API

            try
            {
                Botik = new Telegram.Bot.TelegramBotClient(key); // initialize API

                try
                {
                    await Botik.SetWebhookAsync("");
                }
                catch (Exception ex) { }

                string settingsPath = Path.Combine(Environment.CurrentDirectory, Path.Combine("config", "Settings.xml"));
                Bot botik_aiml = new Bot();
                botik_aiml.loadSettings(settingsPath);
                User myUser = new User("User", botik_aiml);
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

                            Request r = new Request(message.Text, myUser, botik_aiml);
                            Result res = botik_aiml.Chat(r);

                            await Botik.SendTextMessageAsync(message.Chat.Id, res.Output);
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
            //  var text = textBox1.Text;
            //if (text != "" && this.bw.IsBusy != true)
            System.IO.StreamReader sr = new System.IO.StreamReader(@"token_Felix.txt");
            this.bw.RunWorkerAsync(sr.ReadLine());
            buttonStart.Text = "Бот запущен...";

        }
    }
}
