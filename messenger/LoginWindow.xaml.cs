using System;
using System.Windows;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using System.Web;

namespace messenger
{
    public partial class LoginWindow : Window
    {
        public Boolean LoginResult;
        private long VKAppID = 5176422;
        public string VKToken;
        public long VKUserId;

        public class VkJsonTokenResponse
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public long user_id { get; set; }
        }

        public void Logout()
        {
            // Очищает все cookies в Internet Explorer. Временное решение
            string Path = Environment.GetFolderPath(Environment.SpecialFolder.Cookies);
            try
            {
                System.IO.Directory.Delete(Path, true);
            }
            catch (Exception)
            { }
        }

        public void GetToken()
        {
            string reqStrTemplate = "https://oauth.vk.com/authorize?client_id={0}&scope=offline,messages,friends&redirect_uri=http://oauth.vk.com/blank.html&display=mobile&response_type=token";
            // Logout(); // Каждый раз при входе спрашивать логин-пароль
            webBrowser.Navigate(string.Format(reqStrTemplate, this.VKAppID));
        }

        public LoginWindow()
        {
            InitializeComponent();
            GetToken();
        }

        public void wbLoadCompleted(object sender, NavigationEventArgs e)
        {
            Uri currentUri = e.Uri;


            string parameters = currentUri.Fragment;
            if (parameters.Length > 0)
            {
                parameters = parameters.Substring(1);
                this.VKToken = HttpUtility.ParseQueryString(parameters).Get("access_token");
                this.VKUserId = Convert.ToInt64(HttpUtility.ParseQueryString(parameters).Get("user_id"));
                this.Close();
            }
            
            // MessageBox.Show("Ошибка при попытке авторизации", "Ошибка");
            // Environment.Exit(0);
        }
    }
}
