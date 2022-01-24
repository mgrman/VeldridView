
using VeldridView;
using VeldridView.WPF;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

[assembly: ExportRenderer(typeof(VeldridView.VeldridViewPanel), typeof(VeldridViewRenderer))]
namespace VeldridView.WPF
{
    public class VeldridViewRenderer : ViewRenderer<VeldridViewPanel, VeldridPanel>
    {

        protected override void OnElementChanged(ElementChangedEventArgs<VeldridViewPanel> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == default)
            {
                return;
            }

            var view = new VeldridPanel();
            view.Run();

            e.NewElement.Window.OnNext(view);
            SetNativeControl(view);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        
    }
}
