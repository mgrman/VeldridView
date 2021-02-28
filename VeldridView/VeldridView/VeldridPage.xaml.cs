using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VeldridView
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VeldridPage : ContentPage
    {
        public VeldridPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private void RestartButton_Clicked(object sender, EventArgs e)
        {
            VeldridView.Print();
        }

        private void RotationSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {

        }

        private void SelectedBarSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {

        }
    }
}
