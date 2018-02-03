using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Input;

namespace Connect
{
    public partial class Options : PhoneApplicationPage
    {
        TouchPoint first;
        public Options()
        {
            InitializeComponent();
            Touch.FrameReported += Touch_FrameReported;

        }
        private void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
            TouchPoint mainTouch = e.GetPrimaryTouchPoint(this);
            if (mainTouch.Action == TouchAction.Down)
                first = mainTouch;
            else if (mainTouch.Action == TouchAction.Up)
            {
                if (mainTouch.Position.X - first.Position.X > 25)
                {
                    string clicktype = "";
                    string imgqual = "";
                    for (int i = 0; i < this.RadioGrid.Children.Count; i++)
                    {
                            RadioButton rb = (RadioButton)this.RadioGrid.Children[i];
                            if (rb.IsChecked == true && rb.GroupName == "ImgQual")
                            {
                               imgqual = (i+1).ToString();
                            }
                            else if (rb.IsChecked == true && rb.GroupName == "ClickType"){
                                clicktype = (i-3).ToString();
                            }
                    }   
                    NavigationService.Navigate(new Uri("/MainPage.xaml?imgqual=" +imgqual +"&" + "clicktype=" + clicktype, UriKind.Relative));
                    Touch.FrameReported -= Touch_FrameReported;
                }
                //myPivot.SelectedIndex--;
            }


        }
    }
}