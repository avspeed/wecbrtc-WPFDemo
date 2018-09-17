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

        private Boolean _webrtcInitialized;
        public Boolean WebRTCInitialized // here, underscore typo
        {
            get { return _webrtcInitialized; }
            set
            {
                _webrtcInitialized = value; // You miss this line, could be ok to do an equality check here to. :)
                OnPropertyChanged("WebRTCInitialized"); // 
            }
        }

        private string _signalingUrl;
        public string SignalingUrl // here, underscore typo
        {
            get { return _signalingUrl; }
            set
            {
                _signalingUrl = value; // You miss this line, could be ok to do an equality check here to. :)
                OnPropertyChanged("SignalingUrl"); // 
            }
        }

        private Boolean _showChat;
        

        public Boolean ShowChat // here, underscore typo
        {
            get { return _showChat; }
            set
            {
                _showChat = value; // You miss this line, could be ok to do an equality check here to. :)
                OnPropertyChanged("ShowChat"); // 
            }
        }

        private Boolean _showSettings;
        public Boolean ShowSettings // here, underscore typo
        {
            get { return _showSettings; }
            set
            {
                _showSettings = value; // You miss this line, could be ok to do an equality check here to. :)
                OnPropertyChanged("ShowSettings"); // 
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
