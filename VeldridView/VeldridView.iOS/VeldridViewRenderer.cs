using Veldrid;
using VeldridView;
using VeldridView.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(VeldridView2), typeof(VeldridViewRenderer))]
namespace VeldridView.iOS
{
    public class VeldridViewRenderer : ViewRenderer<VeldridView2, UIViewApplicationWindow>
    {
        public VeldridViewRenderer( ) : base()
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<VeldridView2> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == default)
            {
                return;
            }

            var view = new UIViewApplicationWindow();
            view.GraphicsDeviceCreated += (g, r, s) => view.Run();
            var app = new SampleApplication(view);

            SetNativeControl(view);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        
    }
}
