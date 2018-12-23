using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FelixTheBot
{
    public partial class Form1 : Form
    {
        BackgroundWorker BackGroundWorker;
        Telegram.Bot.TelegramBotClient Bot;

        public Form1()
        {
            InitializeComponent();

            
            this.BackGroundWorker = new BackgroundWorker();
            this.BackGroundWorker.DoWork += this.bw_DoWork;
        }

        async void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var key = e.Argument as String; // get bot token

            System.Net.WebProxy wp = new System.Net.WebProxy("54.37.65.79:3128", true);
            wp.Credentials = new System.Net.NetworkCredential("user1", "user1Password");
            Bot = new Telegram.Bot.TelegramBotClient(key, wp); // initialize API

            try
            {
                Bot = new Telegram.Bot.TelegramBotClient(key); // initialize API

                try
                {
                    await Bot.SetWebhookAsync("");
                }
                catch (Exception ex) { }

            int offset = 0; // message indent
                while (true)
                {
                    var updates = await Bot.GetUpdatesAsync(offset);

                    foreach (var update in updates) 
                    {
                        var message = update.Message;
                        if (message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                        {
                            if (message.Text == "/saysomething")
                            {                     
                                await Bot.SendTextMessageAsync(message.Chat.Id, "hey hows it going bros!",
                                       replyToMessageId: message.MessageId);
                            }
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
            var text = textBox1.Text;
            if (text != "" && this.BackGroundWorker.IsBusy != true)
            {
                this.BackGroundWorker.RunWorkerAsync(text);
                buttonStart.Text = "Бот запущен...";
            }
        }
    }
}
