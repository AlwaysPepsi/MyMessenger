using System;
using System.Windows;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace messenger
{
    public partial class LoginWindow : Window
    {
        public Boolean LoginResult;
        private long VKAppID = 5176422;
        private string VKSecretKey = "FC0hXpZ3wbz8jDT97t85";
        public string VKToken;
        public long VKUserId;

        public class VkJsonTokenResponse
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public long user_id { get; set; }
        }


        /*
        private static void SaveTokens()
        {
            XmlSerializer s = new XmlSerializer(typeof(string));
            FileStream fs = new FileStream(Publics.CurrPath + "vk.xml", FileMode.OpenOrCreate);
            s.Serialize(fs, Token);
            fs.Flush();
        }

        public static void LoadTokens()
        {
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(string));
                FileStream fs = new FileStream(Publics.CurrPath + "vk.xml", FileMode.Open);
                Token = (string)s.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Error.SaveError("Vk.LoadTokens", ex.Message);
            }
        }
        */
        public void GetCode()
        {
            string reqStrTemplate = "http://api.vkontakte.ru/oauth/authorize?client_id={0}&scope=offline,messages,friends";
            webBrowser.Navigate(string.Format(reqStrTemplate, this.VKAppID));
        }

        public void GetToken(string Code)
        {
            string reqStrTemplate = "https://api.vkontakte.ru/oauth/access_token?client_id={0}&client_secret={1}&code={2}";
            string reqStr = string.Format(reqStrTemplate, this.VKAppID, this.VKSecretKey, Code);
            WebClient webClient = new WebClient();
            string response = webClient.DownloadString(reqStr);
            VkJsonTokenResponse jsonResponse = JsonConvert.DeserializeObject<VkJsonTokenResponse>(response);
            this.VKToken = jsonResponse.access_token;
            this.VKUserId = jsonResponse.user_id;
        }

        public LoginWindow()
        {
            InitializeComponent();
            GetCode();
        }

        public void wbLoadCompleted(object sender, NavigationEventArgs e)
        {
            // TODO: https://msdn.microsoft.com/ru-ru/library/system.uri.aspx
            Uri currentUri = e.Uri;
            string currentUriStr = currentUri.ToString();
            int pos = currentUriStr.IndexOf("code");
            if (pos > -1) {
                pos += "code=".Length;
                string code = currentUriStr.Substring(pos);
                GetToken(code);
                this.Close();
            }
        }
    }
}
