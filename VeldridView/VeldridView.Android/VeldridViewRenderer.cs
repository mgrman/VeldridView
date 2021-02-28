using Android.Content;
using Veldrid;
using VeldridView;
using VeldridView.Android;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(VeldridView2), typeof(VeldridViewRenderer))]
namespace VeldridView.Android
{
    public class VeldridViewRenderer : ViewRenderer<VeldridView2, VeldridSurfaceView>
    {
        public VeldridViewRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<VeldridView2> e)
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
            var view = new VeldridSurfaceView(Context, backend, options);
            var window = new AndroidApplicationWindow(view);
            window.GraphicsDeviceCreated += (g, r, s) => window.Run();
            var app = new SampleApplication(window);

            SetNativeControl(view);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        
    }
}
