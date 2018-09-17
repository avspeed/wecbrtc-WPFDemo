using System.Windows.Documents;

namespace AppRTCDemo
{
    public class CurrentParticipants
    {
        /// <summary>
        /// The Session that can be used to view the user
        /// </summary>
        public string Session { get; set; }

        /// <summary>
        /// The User name of the participant
        /// </summary>
        public string UserName { get; set; }

        public int ElementPosition { get; set; }

        public iConfRTCWPF.RTCControl Viewer { get; set; }

        public AdornerLayer UserAdornerLayer;
    }

    public class MyUser
    {
        public string UserName { get; set; }
    }
}
