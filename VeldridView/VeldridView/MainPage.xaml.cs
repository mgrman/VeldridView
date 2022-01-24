using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VeldridView
{
    public partial class MainPage : ContentPage
    {
        private SampleApplication? app;

        public MainPage()
        {
            InitializeComponent();


            this.VeldridView.Window.Subscribe(window =>
            {
                if (window == null)
                {
                    return;
                }
                app = new SampleApplication(window);
            });
        }
    }
}
