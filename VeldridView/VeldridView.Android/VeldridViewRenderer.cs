using Android.Content;
using Veldrid;
using VeldridView;
using VeldridView.Android;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(VeldridView.VeldridViewPanel), typeof(VeldridViewRenderer))]
namespace VeldridView.Android
{
    public class VeldridViewRenderer : ViewRenderer<VeldridViewPanel, VeldridSurfaceView>
    {
        public VeldridViewRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<VeldridViewPanel> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == default)
            {
                return;
            }

            var debug = false;
#if DEBUG
            debug = true;
#endif
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug,
                PixelFormat.R16_UNorm,
                false,
                ResourceBindingModel.Improved,
                true,
                true);
            GraphicsBackend backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                ? GraphicsBackend.Vulkan
                : GraphicsBackend.OpenGLES;
            var window = new VeldridSurfaceView(Context!, backend, options);

            e.NewElement.Window.OnNext(window);
            SetNativeControl(window);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        
    }
}
