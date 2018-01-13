using AppRTCDemo.Commands;
using AppRTCDemo.Model;
using AVSPEED;
using iConfRTCModel;
using iConfRTCWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AppRTCDemo
{
    /*
 █████╗ ██╗   ██╗███████╗██████╗ ███████╗███████╗██████╗ 
██╔══██╗██║   ██║██╔════╝██╔══██╗██╔════╝██╔════╝██╔══██╗
███████║██║   ██║███████╗██████╔╝█████╗  █████╗  ██║  ██║
██╔══██║╚██╗ ██╔╝╚════██║██╔═══╝ ██╔══╝  ██╔══╝  ██║  ██║
██║  ██║ ╚████╔╝ ███████║██║     ███████╗███████╗██████╔╝
╚═╝  ╚═╝  ╚═══╝  ╚══════╝╚═╝     ╚══════╝╚══════╝╚═════╝ 
        
        iConfRTC WPF Demo
        A demo similar to the AppRTC demo, but that uses the iConfRTC SDK.

        Feedback, comments
        Email us at : support@avspeed.com

 */
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AppModel model;

        /// <summary>
        /// the iConfRTC Control
        /// </summary>
        private RTCControl rtcControl;

        protected List<CurrentParticipants> currParticipants;

        private List<CustomAdorner> adorners;


        public MainWindow()
        {
            InitializeComponent();

            // !!we initialize webrtc - mandatory call!!
            RTC.Init();

            rtcControl = new RTCControl();
            
            currParticipants = new List<CurrentParticipants>();

            //assign iConfRTC events
            rtcControl.ErrorConnectSignaling += RtcControl_ErrorConnectSignaling;
            rtcControl.ConnectedToSignaling += RtcControl_ConnectedToSignaling;
            rtcControl.RTCInitialized += WebRTCInitialized;
            rtcControl.UserJoinedMeeting += RtcControl_UserJoinedMeeting;
            rtcControl.UserLeftMeeting += RtcControl_UserLeftMeeting;
            rtcControl.ILeftMeeting += RtcControl_ILeftMeeting;
            rtcControl.IJoinedMeeting += RtcControl_IJoinedMeeting;
            rtcControl.MeetingMessageReceived += RtcControl_MeetingMessageReceived;

            model = new AppModel();

            DataContext = model;

            model.PropertyChanged += Model_PropertyChanged;

            model.AppTitle = Const.title;

            
            //set the initial signaling url .. this is where our signalr server is running from
            model.SignalingUrl = System.Configuration.ConfigurationManager.AppSettings["SignalingUrl"];

            rtcControl.SignalingType = SignalingTypes.Socketio;//SignalingTypes.SignalR; // SignalingTypes.Socketio;
            rtcControl.SignalingUrl = model.SignalingUrl;

            SetupCommands();

            ShowMessage("Initializing WebRTC", System.Windows.Media.Brushes.MediumBlue, true);

            rtcControl.Width = 480;
            rtcControl.Height = 360;

            //we add our own video to the list of videos
            videoList.Children.Add(rtcControl);

            
        }

      
        #region iConfRTC Event Handlers

        private void RtcControl_IJoinedMeeting(object sender, UserArgs e)
        {

            model.AppTitle = Const.title + " - In Meeting " + e.MeetingID;
            ShowMessage("Welcome to Meeting : " + e.MeetingID, System.Windows.Media.Brushes.DarkSlateBlue, true);

            AttachLoadingAdorner(rtcControl, e.UserName, e.Session);

            //start viewing sessions other than e.Session whcih is my session

            ProcessParticipants(e.Participants);

        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        private void WebRTCInitialized(object sender)
        {
            //you can add a turn server here
            //rtcControl.AddIceServer(url: "numb.viagenie.ca", userName: "support@avspeed.com", password: "avspeedwebrtc", clearFirst: false, type: "turn");

            model.WebRTCInitialized = true;
            HideMessage();

            rtcControl.SetMutePoster(Const.imageAway);

            //rtcControl.AddIceServer("stun.voiparound.com");
        }

        private void RtcControl_ConnectedToSignaling(object sender, string hubId)
        {
            ShowMessage("Connected To Signaling..", brush: System.Windows.Media.Brushes.Green, stay: false, fontSize: 14);

            //move to second screen
            rtcControl.Visibility = System.Windows.Visibility.Visible;
            gridLanding.Visibility = System.Windows.Visibility.Hidden;
            gridHead.Visibility = System.Windows.Visibility.Hidden;
        }

        private void RtcControl_ErrorConnectSignaling(object sender, string error)
        {
            ShowMessage("Error connecting to Signaling server. : " + error, brush: System.Windows.Media.Brushes.IndianRed, stay: false, fontSize: 14);
        }


        private void RtcControl_MeetingMessageReceived(object sender, MeetingMessageEventArgs e)
        {
            if (!gridChat.IsVisible)
                gridChat.Visibility = System.Windows.Visibility.Visible;

            chatLog.AppendText(e.FromUser + " : " + e.Message + Environment.NewLine);
        }

        private void RtcControl_ILeftMeeting(object sender, UserArgs e)
        {

            foreach (var item in currParticipants)
            {
                var viewerControl = item.Viewer;

                videoList.Children.RemoveAt(item.ElementPosition);
                viewerControl = null;
            }

            currParticipants.Clear();

            rtcControl.Visibility = System.Windows.Visibility.Hidden;
            gridLanding.Visibility = System.Windows.Visibility.Visible;
            gridHead.Visibility = System.Windows.Visibility.Visible;
            model.AppTitle = Const.title;
            RemoveAdorner(rtcControl, e.Session);
            ShowMessage("You left the Meeting : " + e.MeetingID);
        }

        private void RtcControl_UserLeftMeeting(object sender, UserArgs e)
        {

            var viewerControl = currParticipants.First(participant => participant.Session == e.Session).Viewer;
            RemoveAdorner(rtcControl, e.Session);
            videoList.Children.RemoveAt(currParticipants.First(participant => participant.Session == e.Session).ElementPosition);
            currParticipants.RemoveAll(x => x.Session == e.Session);

            viewerControl = null;

        }


        private void RtcControl_UserJoinedMeeting(object sender, UserArgs e)
        {
            //the new peer session available event is fired 
            //a peer joins your conference room or when you join the conference room
            //and are not alone in the conference room
            ProcessParticipants(e.Participants);
        }

        #endregion

        #region Adorners
        private void AttachLoadingAdorner(UIElement el, string textToDisplay, string session)
        {
            CustomAdorner textOverlay = new CustomAdorner(el);
            textOverlay.FontSize = 15;
            textOverlay.OverlayedText = textToDisplay;
            textOverlay.Tag = session;
            textOverlay.Typeface = new Typeface(FontFamily, FontStyles.Normal,
                FontWeights.Bold, FontStretch);
            AdornerLayer.GetAdornerLayer(el).Add(textOverlay);

            if (adorners is null)
            {
                adorners = new List<CustomAdorner>();
            }

            adorners.Add(textOverlay);
        }

        private void RemoveAdorner(UIElement el, string session)
        {
            var adorner = adorners.Find(x => x.Tag.ToString() == session);
            AdornerLayer.GetAdornerLayer(el).Remove(adorner);

            adorners.Remove(adorner);
        }
        #endregion

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (rtcControl.MyMeeting != String.Empty)
            {
                rtcControl.LeaveMeeting();
            }
            e.Cancel = false;
        }

        private void SetupCommands()
        {
            var commands = new List<Command>();

            Command command = new Command();
            command.Obj = imgChat;
            command.Execute += delegate () {
                model.ShowChat = !model.ShowChat;
            };
            commands.Add(command);

            command = new Command();
            command.Obj = imgMuteAudio;
            command.Execute += delegate () {
                model.AudioMuted = !model.AudioMuted;
                rtcControl.MuteAudio(model.AudioMuted);
            };
            commands.Add(command);

            command = new Command();
            command.Obj = imgMuteVideo;
            command.Execute += delegate () {
                model.VideoMuted = !model.VideoMuted;
                rtcControl.MuteVideo(model.VideoMuted);
            };
            commands.Add(command);


            command = new Command();
            command.Obj = imgLeave;
            command.Execute += delegate () {
                rtcControl.LeaveMeeting();
            };
            commands.Add(command);



            foreach (var item in commands)
            {
                CommandHelper c = new CommandHelper((UIElement)item.Obj);
                c.AddCommand(item);
            }
        }

        private void btnJoinMeeting_Click(object sender, RoutedEventArgs e)
        {
            rtcControl.JoinMeeting(model.UserName, model.MeetingID);
        }

       /// <summary>
       /// This is where we go through each participant and setup a viewer which is another instance of the iConfRTC control
       /// </summary>
       /// <param name="participants">the list of participants of the meeting</param>
        private void ProcessParticipants( List<MeetingParticipants> participants)
        {
            foreach (var participant in participants)
            {
                if ( participant.Session != rtcControl.MySession) {
                    RTCControl viewer;
                    viewer = new RTCControl();
                    viewer.SignalingType = SignalingTypes.Socketio;// SignalingTypes.SignalR;// SignalingTypes.Socketio;
                    viewer.SignalingUrl = model.SignalingUrl;
                    viewer.Height = 360;
                    viewer.Width = 480;


                    viewer.Visibility = System.Windows.Visibility.Visible;
                    viewer.ToolTip = participant.UserName;
                    viewer.MySession = participant.Session;



                    //check to see if we are already viewing the session
                    foreach (object item in videoList.Children)
                    {
                        if (item.GetType() == typeof(RTCControl))
                        {
                            var session = (item as RTCControl).MySession;
                            if (session != null)
                                if (session == participant.Session)
                                    return;
                                else continue;
                        }

                    }

                    int elementPosition = videoList.Children.Add(viewer);
                    currParticipants.Add(new CurrentParticipants { Session = participant.Session, UserName = participant.UserName, ElementPosition = elementPosition, Viewer = viewer });

                    MainWindow1.UpdateLayout();
                    AttachLoadingAdorner(viewer, participant.UserName, participant.Session);


                    ///only call webrtc functions when WebRTC is ready!!
                    viewer.RTCInitialized += (((object a) =>
                    {
                        //you can add a turn server here
                        //viewer.AddIceServer(url: "numb.viagenie.ca", userName: "support@avspeed.com", password: "avspeedwebrtc", clearFirst: false, type: "turn");
                        //viewer.AddIceServer("stun.voiparound.com");

                    //webrtc is ready, connect to signaling
                    string ranUser = Helper.RandomString(8);
                        viewer.ViewSession(participant.Session);
                    }));
                }
            }
            
        }

        private void txtMeetingID_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.btnJoinMeeting.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                e.Handled = true;
            }
        }

        private void chatText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (chatText.Text == "Type here")
            {
                chatText.Text = "";
            }
        }

        private void chatText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (chatText.Text.Trim() == "")
            {
                chatText.Text = "Type here";
            }
        }

        private void chatText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string textToSend = chatText.Text.Trim();
                if (textToSend != "")
                {
                    rtcControl.SendMessageToMeeting(textToSend);
                    chatLog.AppendText("You : " + textToSend + Environment.NewLine);
                    chatText.Clear();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Show message at the top of window
        /// </summary>
        /// <param name="message">message to show</param>
        /// <param name="brush"> back color </param>
        private void ShowMessage(string message, System.Windows.Media.Brush brush = null, bool stay = false, Double fontSize = 29.0)
        {
            if (brush != null)
                gridMessage.Background = brush;
            else gridMessage.Background = Brushes.Green;

            gridMessage.Visibility = Visibility.Visible;

            lblMessage.FontSize = fontSize;

            lblMessage.Content  = message;

            if (stay) return;

            var timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(3000)};

            timer.Tick += delegate (object s, EventArgs a)
            {
                gridMessage.Visibility = Visibility.Hidden;

                lblMessage.Content = String.Empty;

                timer.Stop();
            };

            // Start the timer
            timer.Start();
        }

        /// <summary>
        /// Hide message displayed at the top of the window in case ShowMessage was called with stay = true .
        /// </summary>
        private void HideMessage()
        {
            gridMessage.Visibility = Visibility.Hidden;
        }
    }

}