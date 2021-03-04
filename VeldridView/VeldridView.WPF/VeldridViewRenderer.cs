
using VeldridView;
using VeldridView.WPF;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

[assembly: ExportRenderer(typeof(VeldridView2), typeof(VeldridViewRenderer))]
namespace VeldridView.WPF
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
            var app = new SampleApplication(view);
            view.Run();

            SetNativeControl(view);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        
    }
}
