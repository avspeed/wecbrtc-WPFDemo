using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppRTCDemo.Model
{
    public class AppModel:INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public string UserName { get; set; }
        public string MeetingID { get; set; }
        public bool AudioMuted { get; set; }
        public bool VideoMuted { get; set; }

        public string _appTitle;
        public string AppTitle
        {
            get { return _appTitle; }
            set
            {
                _appTitle = value;
                OnPropertyChanged("AppTitle");
            }
        }

        private Boolean _webrtcInitialized;
        public Boolean WebRTCInitialized 
        {
            get { return _webrtcInitialized; }
            set
            {
                _webrtcInitialized = value; 
                OnPropertyChanged("WebRTCInitialized"); 
            }
        }

        private string _signalingUrl;
        public string SignalingUrl 
        {
            get { return _signalingUrl; }
            set
            {
                _signalingUrl = value; 
                OnPropertyChanged("SignalingUrl"); // 
            }
        }

        private Boolean _showChat;
        

        public Boolean ShowChat 
        {
            get { return _showChat; }
            set
            {
                _showChat = value; 
                OnPropertyChanged("ShowChat"); 
            }
        }

        private Boolean _showSettings;
        public Boolean ShowSettings 
        {
            get { return _showSettings; }
            set
            {
                _showSettings = value; 
                OnPropertyChanged("ShowSettings"); 
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
