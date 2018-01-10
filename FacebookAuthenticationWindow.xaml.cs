using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AppRTCDemo
{
    public class FacebookUser
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    /// <summary>
    /// Interaction logic for FacebookAuthenticationWindow.xaml
    /// </summary>
    public partial class FacebookAuthenticationWindow : Window
    {
        //The Application ID from Facebook
        public string AppID { get; set; }

        //The access token retrieved from facebook's authentication
        public string AccessToken { get; set; }

        public string FullName { get; set; }

        public FacebookAuthenticationWindow()
        {
            InitializeComponent();
            this.Loaded += (object sender, RoutedEventArgs e) =>
            {
                //Add the message hook in the code behind since I got a weird bug when trying to do it in the XAML
                webBrowser.MessageHook += webBrowser_MessageHook;

                //Delete the cookies since the last authentication
                DeleteFacebookCookie();

                //Create the destination URL
                var destinationURL = String.Format("https://www.facebook.com/dialog/oauth?client_id={0}&scope={1}&display=popup&redirect_uri=http://www.facebook.com/connect/login_success.html&response_type=token",
                   AppID, //client_id
                   "email,user_birthday" //scope
                );
                webBrowser.Navigate(destinationURL);
            };
        }

        private void webBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //If the URL has an access_token, grab it and walk away...
            var url = e.Uri.Fragment;
            if (url.Contains("access_token") && url.Contains("#"))
            {
                url = (new System.Text.RegularExpressions.Regex("#")).Replace(url, "?", 1);
                AccessToken = System.Web.HttpUtility.ParseQueryString(url).Get("access_token");

                string posturl = "https://graph.facebook.com/me?access_token=" + AccessToken;

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(posturl);
                request.Method = "GET";

                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseString = reader.ReadToEnd();

                reader.Close();

                var fbUser = JsonConvert.DeserializeObject<FacebookUser>(responseString);

                FullName = fbUser.name;


                DialogResult = true;
                this.Close();
            }
        }

        private void DeleteFacebookCookie()
        {
            //Set the current user cookie to have expired yesterday
            string cookie = String.Format("c_user=; expires={0:R}; path=/; domain=.facebook.com", DateTime.UtcNow.AddDays(-1).ToString("R"));
            Application.SetCookie(new Uri("https://www.facebook.com"), cookie);
        }

        private void webBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri.LocalPath == "/r.php")
            {
                MessageBox.Show("To create a new account go to www.facebook.com", "Could Not Create Account", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
        }

        IntPtr webBrowser_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //msg = 130 is the last call for when the window gets closed on a window.close() in javascript
            if (msg == 130)
            {
                this.Close();
            }
            return IntPtr.Zero;
        }
    }
}
