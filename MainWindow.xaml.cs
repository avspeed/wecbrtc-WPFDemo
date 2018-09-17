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
        RTCControl rtcControl;

        protected List<CurrentParticipants> currParticipants;

        private CustomAdorner myAdorner;

        public MainWindow()
        {
            InitializeComponent();

            //initilize webrtc
            RTC.Init();

            rtcControl = new RTCControl();

            currParticipants = new List<CurrentParticipants>();

            //setup events
            rtcControl.ErrorConnectSignaling += RtcControl_ErrorConnectSignaling;
            rtcControl.ConnectedToSignaling += RtcControl_ConnectedToSignaling;
            rtcControl.RTCInitialized += WebRTCInitialized;
            rtcControl.UserJoinedMeeting += RtcControl_UserJoinedMeeting;
            rtcControl.UserLeftMeeting += RtcControl_UserLeftMeeting;
            rtcControl.ILeftMeeting += RtcControl_ILeftMeeting;
            rtcControl.IJoinedMeeting += RtcControl_IJoinedMeeting;
            rtcControl.MeetingMessageReceived += RtcControl_MeetingMessageReceived;


            model = new AppModel();

            this.DataContext = model;

            model.PropertyChanged += Model_PropertyChanged;


            //set the initial signaling url .. this is where our signalr server is running from
            model.SignalingUrl = System.Configuration.ConfigurationManager.AppSettings["SignalingUrl"];

            rtcControl.SignalingType = SignalingTypes.Socketio; //SignalingTypes.SignalR; // SignalingTypes.Socketio;
            rtcControl.SignalingUrl = model.SignalingUrl;

            SetupCommands();

            ShowMessage("Initializing WebRTC", System.Windows.Media.Brushes.MediumBlue, true);

            rtcControl.Width = 480;
            rtcControl.Height = 360;

            //we add our own video to the list of videos
            videoList.Children.Add(rtcControl);

            //we initialize webrtc - mandatory call!
        }

        private CustomAdorner AttachLoadingAdorner(UIElement el, string textToDisplay)
        {
            CustomAdorner loading = new CustomAdorner(el);
            loading.FontSize = 15;
            loading.OverlayedText = textToDisplay;

            loading.Typeface = new Typeface(FontFamily, FontStyles.Normal,
                FontWeights.Bold, FontStretch);

            var layer = AdornerLayer.GetAdornerLayer(el);

            layer.Add(loading);

            return loading;
        }

        private void RtcControl_IJoinedMeeting(object sender, UserArgs e)
        {
            ShowMessage("Welcome to Meeting : " + e.MeetingID, System.Windows.Media.Brushes.DarkSlateBlue, true);

            myAdorner = AttachLoadingAdorner(rtcControl, e.UserName);
            

            //start viewing sessions other than e.Session whcih is my session

            ProcessParticipants(e.Participants, e.Session, e.Sharing);
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        private void RtcControl_ConnectedToSignaling(object sender, string hubId)
        {
            ShowMessage("Connected To Signaling..", brush: System.Windows.Media.Brushes.Green, stay: false,
                fontSize: 14);

            //move to second screen
            rtcControl.Visibility = System.Windows.Visibility.Visible;
            gridLanding.Visibility = System.Windows.Visibility.Hidden;
            gridHead.Visibility = System.Windows.Visibility.Hidden;
        }

        private void RtcControl_ErrorConnectSignaling(object sender, string error)
        {
            ShowMessage("Error connecting to Signaling server. : " + error,
                brush: System.Windows.Media.Brushes.IndianRed, stay: false, fontSize: 14);
        }


        private void RtcControl_MeetingMessageReceived(object sender, MeetingMessageEventArgs e)
        {
            if (!gridChat.IsVisible)
                gridChat.Visibility = System.Windows.Visibility.Visible;

            if (e.FromUser != rtcControl.MyUserName)
            {
                chatLog.AppendText(e.FromUser + " : " + e.Message + Environment.NewLine);
            }
        }

        private void RtcControl_ILeftMeeting(object sender, UserArgs e)
        {
            foreach (var item in currParticipants)
            {
                videoList.Children.RemoveAt(item.ElementPosition);
                //viewerControl = null;
            }

            currParticipants.Clear();
            var layer = AdornerLayer.GetAdornerLayer(rtcControl);

            layer.Remove(myAdorner);

            rtcControl.Visibility = System.Windows.Visibility.Hidden;
            gridLanding.Visibility = System.Windows.Visibility.Visible;
            gridHead.Visibility = System.Windows.Visibility.Visible;

            ShowMessage("You left the Meeting : " + e.MeetingID);
        }

        private void RtcControl_UserLeftMeeting(object sender, UserArgs e)
        {
            try
            {
                if (currParticipants == null || currParticipants.Count == 0)
                    return;

                var currParticipant = currParticipants.First(participant => participant.Session == e.Session);

                if ((currParticipant != null)
                    && (videoList.Children[currParticipant.ElementPosition] != null))
                {
                    videoList.Children.RemoveAt(currParticipant.ElementPosition);
                }

                currParticipants.RemoveAll(x => x.Session == e.Session);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }


        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (rtcControl.MyMeetingID != String.Empty)
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
            command.Execute += delegate() { model.ShowChat = !model.ShowChat; };
            commands.Add(command);

            command = new Command();
            command.Obj = imgMuteAudio;
            command.Execute += delegate()
            {
                model.AudioMuted = !model.AudioMuted;
                rtcControl.MuteAudio(model.AudioMuted);
            };
            commands.Add(command);

            command = new Command();
            command.Obj = imgMuteVideo;
            command.Execute += delegate()
            {
                model.VideoMuted = !model.VideoMuted;
                rtcControl.MuteVideo(model.VideoMuted);
            };
            commands.Add(command);


            command = new Command();
            command.Obj = imgLeave;
            command.Execute += delegate() { rtcControl.LeaveMeeting(); };
            commands.Add(command);


            foreach (var item in commands)
            {
                CommandHelper c = new CommandHelper((UIElement) item.Obj);
                c.AddCommand(item);
            }
        }

        private void btnJoinMeeting_Click(object sender, RoutedEventArgs e)
        {
            rtcControl.JoinMeeting(model.UserName, model.MeetingID);
        }

        private void WebRTCInitialized(object sender)
        {
            //you can add a turn server here
            //rtcControl.AddIceServer(url: "numb.viagenie.ca", userName: "support@avspeed.com", password: "avspeedwebrtc", clearFirst: false, type: "turn");

            model.WebRTCInitialized = true;
            HideMessage();

            // rtcControl.SetMutePoster(Const.imageAway);

            //rtcControl.AddIceServer("stun.voiparound.com");
        }

        void gridBigVideo_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        //void h_InConference(iConfRTC.Shared.ConferenceEventArgs e)
        //{
        //    ShowMessage((e.userName == model.UserName ? "You " : e.userName) + " joined " + e.conferenceId);
        //    // MessageBox.Show("You (" + e.userName + ") have joined  conference  + e.conferenceId);
        //    h.EnableOverlay("In conference : " + e.conferenceId, "position:absolute;bottom:0px;color:#FFF;text-align:center;font-size:20px;background-color:rgba(221,221,221,0.3);width:640px;padding:10px0;z-index:2147483647;font-family: Verdana, Geneva, sans-serif;", true);
        //}

        private void ProcessParticipants(List<MeetingParticipants> participants, string sessionJoined, bool isSharing)
        {
            foreach (var participant in participants)
            {
                if (participant.Session != rtcControl.MySession) //you are already seeing yourself :)
                {
                    var sessionExists = currParticipants.Any(p => p.Session == participant.Session);

                    if (!sessionExists)
                    {
                        var viewer = new RTCControl
                        {
                            SignalingType = SignalingTypes.Socketio,
                            SignalingUrl = model.SignalingUrl,
                            Height = 360,
                            Width = 480,
                            Visibility = System.Windows.Visibility.Visible,
                            ToolTip = participant.UserName,
                            MySession = participant.Session
                        };

                        int elementPosition = videoList.Children.Add(viewer);
                        currParticipants.Add(new CurrentParticipants
                        {
                            Session = participant.Session,
                            UserName = participant.UserName,
                            ElementPosition = elementPosition,
                            Viewer = viewer
                        });

                        MainWindow1.UpdateLayout();
                        AttachLoadingAdorner(viewer, participant.UserName);

                        //only call webrtc functions when WebRTC is ready!!
                        viewer.RTCInitialized += (((object a) =>
                        {
                            //you can add a turn server here
                            //viewer.AddIceServer(url: "numb.viagenie.ca", userName: "support@avspeed.com", password: "avspeedwebrtc", clearFirst: false, type: "turn");
                            //viewer.AddIceServer("stun.voiparound.com");

                            //webrtc is ready, connect to signaling
                            viewer.ViewSession(participant.UserName, participant.Session);
                        }));
                    }
                }
            }
        }

        private void RtcControl_UserJoinedMeeting(object sender, UserArgs e)
        {
            //the new peer session available event is fired 
            //a peer joins your conference room or when you join the conference room
            //and are not alone in the conference room
            ProcessParticipants(e.Participants, e.Session, e.Sharing);
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
        private void ShowMessage(string message, System.Windows.Media.Brush brush = null, bool stay = false,
            Double fontSize = 29.0)
        {
            if (brush != null)
                gridMessage.Background = brush;
            else gridMessage.Background = System.Windows.Media.Brushes.Green;

            gridMessage.Visibility = Visibility.Visible;

            lblMessage.FontSize = fontSize;

            lblMessage.Content = message;

            if (!stay)
            {
                var _timer = new DispatcherTimer();

                _timer.Interval = TimeSpan.FromMilliseconds(3000);

                _timer.Tick += new EventHandler(delegate(object s, EventArgs a)
                {
                    gridMessage.Visibility = Visibility.Hidden;

                    lblMessage.Content = String.Empty;

                    _timer.Stop();
                });

                // Start the timer
                _timer.Start();
            }
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