using MyX3DParser.Generated.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VeldridView
{
    public partial class MainPage : ContentPage
    {
        private SampleApplication activeApp;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            var result =await PickX3D();
            if (result == null)
            {
                return;
            }

            var stream =await result.OpenReadAsync();

            var xml = new XmlDocument();
            xml.Load(stream);
            var x3d = MyX3DParser.Generated.Model.Parsing.Parser.Parse_X3D(xml.DocumentElement, new X3DContext());


            if (activeApp == null)
            {
                activeApp = new SampleApplication(this.VeldridView.Window);

            }

            activeApp.LoadX3d(x3d);

        }

        async Task<FileResult> PickX3D()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                   {
                    { DevicePlatform.iOS, new[] { "x3d"  } }, // or general UTType values
                    { DevicePlatform.Android, new[] { "model/x3d+xml" } },
                    { DevicePlatform.UWP, new[] { ".x3d" } },
                    { DevicePlatform.macOS, new[] { "x3d" } }, // or general UTType values
                   });
                var options = new PickOptions
                {
                    PickerTitle = "Please select an X3D file",
                    FileTypes = customFileType,
                };

                var result = await FilePicker.PickAsync();
                return result;
            }
            catch (Exception ex)
            {
                // The user canceled or something went wrong
                return null;
            }
        }
    }
}
