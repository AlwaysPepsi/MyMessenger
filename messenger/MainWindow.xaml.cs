using System.Windows;
using System.Net;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Animation;


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

        public string Api(string reqStr)
        {   
            WebClient webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.UTF8;
            return webClient.DownloadString(reqStr);
        }

        public string SendMessage(long userId, string message)
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/messages.send?user_id={0}&access_token={1}&message={2}";
            string reqStr = string.Format(reqStrTemplate, userId, this.VKToken, message);
            return Api(reqStr);
        }

        public VKFriend[] GetFriends()
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/friends.get?access_token={0}&order=hints&fields=nickname&count=100";
            string reqStr = string.Format(reqStrTemplate, this.VKToken);
            string response = Api(reqStr);
            VKFriendsResponse jsonResponse = JsonConvert.DeserializeObject<VKFriendsResponse>(response);
            return jsonResponse.response;
        }

        public VKMessage[] GetMessages(long userId)
        {
            string reqStrTemplate = "https://api.vkontakte.ru/method/messages.getHistory?access_token={0}&user_id={1}&v=5.38";
            string reqStr = string.Format(reqStrTemplate, this.VKToken, userId);
            string response = Api(reqStr);
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
            VKFriend friend = getCurrentUser();
            string message = textBoxNewMessage.Text;
            if (friend == null)
            {
                MessageBox.Show("Не выбран ни один из друзей", "Ошибка");
                return;
            }
            if (message == null || message == "")
            {
                MessageBox.Show("Пустое сообщение", "Ошибка");
                return;
            }
            SendMessage(friend.user_id, message);
            dialog.addMessage(message, true);
            textBoxNewMessage.Clear();
        }

        public MainWindow()
        {
            LoginWindow loginWin = new LoginWindow();
            loginWin.ShowDialog();
            VKToken = loginWin.VKToken;
            VKUserId = loginWin.VKUserId;
            if (VKToken == null)
                Environment.Exit(0);

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

            private Dictionary<string, string> _mappings = new Dictionary<string, string>();

            private string GetEmoticonText(string text)
            {
                string match = string.Empty;
                int lowestPosition = text.Length;

                foreach (KeyValuePair<string, string> pair in _mappings)
                {
                    if (text.Contains(pair.Key))
                    {
                        int newPosition = text.IndexOf(pair.Key);
                        if (newPosition < lowestPosition)
                        {
                            match = pair.Key;
                            lowestPosition = newPosition;
                        }
                    }
                }

                return match;

            }
             
            private void Emoticons(string msg, Paragraph para)
            {
                Run r = new Run(msg);

                para.Inlines.Add(r);

                string emoticonText = GetEmoticonText(r.Text);

                //if paragraph does not contains smile only add plain text to richtextbox rtb2
                if (string.IsNullOrEmpty(emoticonText))
                {
                    richBox.Document.Blocks.Add(para);
                }
                else
                {
                    while (!string.IsNullOrEmpty(emoticonText))
                    {
                        TextPointer tp = r.ContentStart;
                        // keep moving the cursor until we find the emoticon text
                        while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith(emoticonText))
                            tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                        // select all of the emoticon text
                        var tr = new TextRange(tp, tp.GetPositionAtOffset(emoticonText.Length)) { Text = string.Empty };
                        //relative path to image smile file
                        string path = _mappings[emoticonText];
                        Image image = new Image
                        {
                            Source = new BitmapImage(new System.Uri(path, UriKind.RelativeOrAbsolute)),
                            Width = 25,
                            Height = 25,
                            Stretch = Stretch.None
                        };
                        

                        //insert smile
                        new InlineUIContainer(image, tp);

                        if (para != null)
                        {
                            var endRun = para.Inlines.LastInline as Run;

                            if (endRun == null)
                            {
                                break;
                            }
                            else
                            {
                                emoticonText = GetEmoticonText(endRun.Text);
                            }

                        }

                    }
                    richBox.Document.Blocks.Add(para);

                }
            }


            public Dialog(RichTextBox richBox, VKFriend friend)
            {
                 this.richBox = richBox;
                 this.friend = friend;
                _mappings.Add(@":)", "http://kolobok.us/smiles/standart/smile3.gif");
                _mappings.Add(@":-)", "http://kolobok.us/smiles/standart/smile3.gif");
                _mappings.Add(@"😊", "http://kolobok.us/smiles/standart/smile3.gif");
                _mappings.Add(@":(", "http://kolobok.us/smiles/standart/sad.gif");
                _mappings.Add(@":-(", "http://kolobok.us/smiles/standart/sad.gif");
                _mappings.Add(@":D", "http://kolobok.us/smiles/standart/grin.gif");
                _mappings.Add(@":-D", "http://kolobok.us/smiles/standart/grin.gif");
                _mappings.Add(@"😂", "http://kolobok.us/smiles/standart/grin.gif");

            }

            public void addMessage(string message, bool myMessage = false)
            {
                var paragraph = new Paragraph();
               

                if (myMessage)
                {
                    paragraph.TextAlignment = System.Windows.TextAlignment.Right;
                }

                richBox.Document.Blocks.Add(paragraph);
                Emoticons(message, paragraph);
                richBox.Focus();
                richBox.ScrollToEnd();
                
            }

            public void openDialog(VKMessage[] messages)
            {
                Array.Reverse(messages);
                richBox.Document.Blocks.Clear();
                foreach (VKMessage message in messages)
                {
                    addMessage(message.body, message.from_id != friend.user_id);
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
