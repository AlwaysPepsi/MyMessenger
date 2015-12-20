using System.Windows;
using System.Net;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using System.Windows.Documents;
using System.Collections.Generic;


namespace messenger
{
    public partial class MainWindow : Window
    {

        private string VKToken;
        public long VKUserId;
        private Dictionary<long, VKFriend> friendsMap;
        private Dialog dialog;

        public class VKFriendsResponse
        {
            public VKFriend[] response { get; set; }
        }

        public class VKFriend
        {
            public string uid { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string nickname { get; set; }
            public int online { get; set; }
            public long user_id { get; set; }

            public override string ToString()
            {
                return first_name + " " + last_name;
            }

            public VKFriend(string first_name)
            {
                this.first_name = first_name;
            }
        }

        public class VKMessagesResponse
        {
            public int count { get; set; }
            public VKMessagesResponseBody response { get; set; }
        }

        public class VKMessagesResponseBody
        {            
            public VKMessage[] items { get; set; }
        }

        public class VKMessage
        {
            public string body { get; set; }
            public long mid { get; set; }
            public string uid { get; set; }
            public long from_id { get; set; }
            public string date { get; set; }
            public int read_state { get; set; }         
        }

        public class VKLongPoolResponse
        {
            public int count { get; set; }
            public VKLongPoolResponseBody response { get; set; }
        }

        public class VKLongPoolResponseBody
        {
            public string key { get; set; }
            public string server { get; set; }
            public string ts { get; set; }
            public string mode { get; set; }
        }

        public string SendMessage(long userId, string message)
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/messages.send?user_id={0}&access_token={1}&message={2}";
            string reqStr = string.Format(reqStrTemplate, userId, this.VKToken, message);
            WebClient webClient = new WebClient();
            return webClient.DownloadString(reqStr);
        }

        public VKFriend[] GetFriends()
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/friends.get?access_token={0}&order=hints&fields=nickname&count=100";
            string reqStr = string.Format(reqStrTemplate, this.VKToken);
            WebClient webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.UTF8;
            string response = webClient.DownloadString(reqStr);
            VKFriendsResponse jsonResponse = JsonConvert.DeserializeObject<VKFriendsResponse>(response);
            return jsonResponse.response;
        }

        public VKMessage[] GetMessages(long userId)
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/messages.getHistory?access_token={0}&user_id={1}&v=5.38";
            string reqStr = string.Format(reqStrTemplate, this.VKToken, userId);
            WebClient webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.UTF8;
            string response = webClient.DownloadString(reqStr);
            VKMessagesResponse jsonResponse = JsonConvert.DeserializeObject<VKMessagesResponse>(response);
            return jsonResponse.response.items;
        }

        public void LongPoolMessages()
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/messages.getLongPollServer?access_token={0}";
            string reqStr = string.Format(reqStrTemplate, this.VKToken);
            WebClient webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.UTF8;
            string response = webClient.DownloadString(reqStr);
            VKLongPoolResponse jsonResponse = JsonConvert.DeserializeObject<VKLongPoolResponse>(response);

            reqStrTemplate = "http://{0}?act=a_check&key={1}&ts={2}&wait=25&mode=2";
            reqStr = string.Format(reqStrTemplate, jsonResponse.response.server, jsonResponse.response.key, jsonResponse.response.ts);
            response = webClient.DownloadString(reqStr);            
        }


        private void buttonSendMessagClick(object sender, RoutedEventArgs e)
        {
            SendMessage(getCurrentUser().user_id, textBoxNewMessage.Text);
            dialog.addMyMessage(textBoxNewMessage.Text);
            textBoxNewMessage.Clear();
        }

        public MainWindow()
        {
            LoginWindow loginWin = new LoginWindow();
            loginWin.ShowDialog();
            VKToken = loginWin.VKToken;
            VKUserId = loginWin.VKUserId;
            if (VKToken == null)
                this.Close();
            InitializeComponent();

            VKFriend[] friends = GetFriends();
            friendsMap = new Dictionary<long, VKFriend>();

            friendsMap.Add(VKUserId, new VKFriend("Я"));
            foreach (VKFriend friend in friends)
            {
                listBoxContactList.Items.Add(friend);
                friendsMap.Add(friend.user_id, friend);
            }

            //LongPoolMessages();

            // Closing += (s, e) => ViewModelLocator.Cleanup();
            BitmapImage bitmapSmiley = new BitmapImage(new Uri("smile.gif", UriKind.Relative));
            
        }

        private VKFriend getCurrentUser()
        {
           return listBoxContactList.SelectedItem as VKFriend;
        }

        private class Dialog
        {
            RichTextBox richBox;
            VKFriend friend;

            public Dialog(RichTextBox richBox, VKFriend friend)
            {
                 this.richBox = richBox;
                 this.friend = friend;
            }

            public void addMessage(string message, string friend)
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run(string.Format("{0}: {1}", friend, message)));
                richBox.Document.Blocks.Add(paragraph);
                richBox.Focus();
                richBox.ScrollToEnd();
            }

            public void addFriendMessage(string message)
            {
                addMessage(message, friend.ToString());
            }

            public void addMyMessage(string message)
            {
                addMessage(message, "Я");
            }

            public void openDialog(VKMessage[] messages)
            {
                Array.Reverse(messages);
                richBox.Document.Blocks.Clear();
                foreach (VKMessage message in messages)
                {
                    if (message.from_id == friend.user_id)
                    {
                        addFriendMessage(message.body);
                    }
                    else
                    {
                        addMyMessage(message.body);
                    }
                }
            }
        }

        private void listBoxContactListSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (listBoxContactList.SelectedIndex >= 0)
            {
                dialog = new Dialog(richTextBoxMessages, getCurrentUser());

                VKMessage[] messages = GetMessages(getCurrentUser().user_id);
                dialog.openDialog(messages);                
            }
        }
    }

}
