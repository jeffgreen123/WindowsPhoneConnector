using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Connect.Resources;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Threading;
using System.Windows.Input;

namespace Connect
{
    public partial class MainPage : PhoneApplicationPage
    {
        Client client;
        TouchPoint first;
        private double m_Zoom = 1;
        private double m_Width = 0;
        private double m_Height = 0;
        private string click = "2";
        // Constructor    
        public MainPage()
        {

            InitializeComponent();
            client = new Client();
            client.Connect("192.168.0.1", 27015);
            DispatcherTimer newTimer = new DispatcherTimer();
            // timer interval specified as 1 second
            newTimer.Interval = TimeSpan.FromSeconds(0.25);
            // Sub-routine OnTimerTick will be called at every 1 second
            newTimer.Tick += OnTimerTick;
            // starting the timer
            newTimer.Start();
            Touch.FrameReported += Touch_FrameReported;


            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }
        void OnTimerTick(Object sender, EventArgs args)
        {
            if(client.getPicReady())
            {
                byte[] byteBuffer = client.getPic();
                MemoryStream memoryStream = new MemoryStream(byteBuffer);
                memoryStream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(memoryStream);
                image.Source = bitmapImage;

                memoryStream.Close();
                memoryStream = null;
                byteBuffer = null;
                client.setPicReady(false);
                Progress.Value = 0;
                Progress.Visibility = Visibility.Collapsed;
            }
                Progress.Value =  100 - client.percent;
            
        }

        private void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
                TouchPoint mainTouch = e.GetPrimaryTouchPoint(image);
                if (mainTouch.Action == TouchAction.Down)
                    first = mainTouch;
                else if (mainTouch.Action == TouchAction.Up)
                {
                    if (mainTouch.Position.X - first.Position.X < -350)
                    {
                        Touch.FrameReported -= Touch_FrameReported;
                        NavigationService.Navigate(new Uri("/Options.xaml?", UriKind.Relative));
                        client.Send("7");
                        
                    }
                    else if(mainTouch.Position.Y - first.Position.Y < -150)
                    {
                        sendType.Visibility = Visibility.Visible;
                        SendButton.Visibility = Visibility.Visible;
                        sendType.Focus();
                    }
                    //myPivot.SelectedIndex--;
                }
               
            
        }  
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string parameter;
            if (NavigationContext.QueryString.TryGetValue("imgqual", out parameter))
            {
                client.Send("6");
                client.Send(parameter);
            }
            if (NavigationContext.QueryString.TryGetValue("clicktype", out parameter))
            {
               click = parameter;
            }
    }

        private void GetImage()
        {
            Progress.Visibility = Visibility.Visible;
            Progress.Value = 100;
            int datasize;
            client.Send("5");
            string size = client.Receive(260);

            bool result = int.TryParse(size.Substring(0, 6), out datasize);
            if (result == false)
            {
                int.TryParse(size.Substring(0, 5), out datasize);
            }
            client.ReceiveImage(datasize);
        }
        private void Flip(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //This code opens up the keyboard when you navigate to the page.
            if (Progress.Visibility == Visibility.Collapsed)
            {
                GetImage();
                
            }
        }
        private void Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sendType.Visibility == Visibility.Collapsed)
            {
                client.Send(click);
                Point p = e.GetPosition(image);
                Point revamped = p;
                revamped.X = revamped.X * 1920 / image.Width;
                revamped.Y = revamped.Y * 1080 / image.Height;
                client.Send((revamped.X.ToString() + "," + revamped.Y.ToString() + new string('x', 260)).Substring(0, 260));
            }
            else
            {
                sendType.Visibility = Visibility.Collapsed;
                SendButton.Visibility = Visibility.Collapsed;
                sendType.Text = "";
            }
        }

        private void image_Loaded(object sender, RoutedEventArgs e)
        {
            image.Width = image.ActualWidth;
            image.Height = image.ActualHeight;
            m_Width = image.Width*3;
            m_Height = image.Height*3;

            // Initaialy we put Stretch to None in XAML part, so we can read image ActualWidth i ActualHeight (otherwise values are strange)
            // After that we set Stretch to UniformToFill in order to be able to resize image
            viewport.Bounds = new Rect(0, 0, image.ActualWidth, image.ActualHeight);
        }


        private void viewport_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation != null)
            {

                double newWidth, newHieght;


                if (m_Width < m_Height)  // box new size between image size and viewport actual size
                {
                    newHieght = m_Height * m_Zoom * e.PinchManipulation.CumulativeScale;
                    newHieght = Math.Max(viewport.ActualHeight, newHieght);
                    newHieght = Math.Min(newHieght, m_Height);
                    newWidth = newHieght * m_Width / m_Height;
                }
                else
                {
                    newWidth = m_Width * m_Zoom * e.PinchManipulation.CumulativeScale;
                    newWidth = Math.Max(viewport.ActualWidth, newWidth);
                    newWidth = Math.Min(newWidth, m_Width);
                    newHieght = newWidth * m_Height / m_Width;
                }


                if (newWidth < m_Width && newHieght < m_Height)
                {
                    // Tells image positione in viewport (offset)
                    MatrixTransform transform = image.TransformToVisual(viewport) as MatrixTransform;
                    // Calculate center of pinch gesture on image (not screen)
                    Point pinchCenterOnImage = transform.Transform(e.PinchManipulation.Original.Center);
                    // Calculate relative point (0-1) of pinch center in image
                    Point relativeCenter = new Point(e.PinchManipulation.Original.Center.X / image.Width, e.PinchManipulation.Original.Center.Y / image.Height);
                    // Calculate and set new origin point of viewport
                    Point newOriginPoint = new Point(relativeCenter.X * newWidth - pinchCenterOnImage.X, relativeCenter.Y * newHieght - pinchCenterOnImage.Y);
                    viewport.SetViewportOrigin(newOriginPoint);
                }

                image.Width = newWidth;
                image.Height = newHieght;

                // Set new view port bound
                viewport.Bounds = new Rect(0, 0, newWidth, newHieght);
            }
        }

        private void viewport_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            m_Zoom = image.Width / m_Width;
        }

        private void SendText(object sender, RoutedEventArgs e)
        {
            string text = sendType.Text;
            client.Send("4");
            string c = (text.Length.ToString() + new string('-', 260)).Substring(0,260);
            int x = c.Length;
            client.Send(c);
            client.Send(text);
            sendType.Visibility = Visibility.Collapsed;
            SendButton.Visibility = Visibility.Collapsed;
            sendType.Text = "";
        }
    }
}