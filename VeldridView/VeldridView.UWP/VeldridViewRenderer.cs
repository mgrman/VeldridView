
using Veldrid;
using VeldridView;
using VeldridView.UWP;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(VeldridView2), typeof(VeldridViewRenderer))]
namespace VeldridView.UWP
{
    public class VeldridViewRenderer : ViewRenderer<VeldridView2, VeldridPanel>
    {

        protected override void OnElementChanged(ElementChangedEventArgs<VeldridView2> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == default)
            {
                return;
            }

            var view = new VeldridPanel();
            view.Run();

            e.NewElement.Window = view;
            SetNativeControl(view);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        
    }
}
