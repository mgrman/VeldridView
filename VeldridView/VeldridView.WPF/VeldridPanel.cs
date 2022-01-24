using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using PixelFormat = Veldrid.PixelFormat;

namespace VeldridView.WPF
{
    public class VeldridPanel : Win32HwndControl, IApplicationWindow
    {
        private GraphicsDevice _gd;
        private Swapchain _sc;
        private readonly Stopwatch _sw;
        private double _previousSeconds;



        private readonly Subject<float> rendering = new();
        private readonly BehaviorSubject<GraphicsDevice?> graphicsDevice = new(null);
        private readonly BehaviorSubject<Swapchain?> swapchain = new(null);
        private readonly BehaviorSubject<(uint width, uint height)> size;

        private Subject<KeyEvent> keyChange = new Subject<KeyEvent>();
        private Subject<PointerEvent> pointerChange = new Subject<PointerEvent>();

        IObservable<float> IApplicationWindow.Rendering => rendering;
        IObservable<GraphicsDevice?> IApplicationWindow.GraphicsDevice => graphicsDevice;
        IObservable<Swapchain?> IApplicationWindow.Swapchain => swapchain;
        IObservable<(uint width, uint height)> IApplicationWindow.Size => size;
        IObservable<KeyEvent> IApplicationWindow.KeyChange => keyChange;
        IObservable<PointerEvent> IApplicationWindow.PointerChange => pointerChange;


        public VeldridPanel()
        {
            _sw = Stopwatch.StartNew();
            size = new BehaviorSubject<(uint width, uint height)>((1000, 1000));
        }

        protected override sealed void Initialize()
        {
            CreateSwapchain();

            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        protected override sealed void Uninitialize()
        {
            CompositionTarget.Rendering -= OnCompositionTargetRendering;

            DestroySwapchain();
        }


        private void OnCompositionTargetRendering(object sender, EventArgs eventArgs)
        {
            if (_gd == null)
            {
                return;
            }

            var newSize = GetSize();
            if (newSize != size.Value)
            {
                _gd.MainSwapchain.Resize(newSize.width, newSize.height);
                size.OnNext(newSize);
            }

            double newSeconds = _sw.Elapsed.TotalSeconds;
            double deltaSeconds = newSeconds - _previousSeconds;
            _previousSeconds = newSeconds;
            rendering.OnNext((float)deltaSeconds);
        }

        private double GetDpiScale()
        {
            PresentationSource source = PresentationSource.FromVisual(this);

            return source.CompositionTarget.TransformToDevice.M11;
        }


        protected virtual void CreateSwapchain()
        {
            Module mainModule = typeof(VeldridPanel).Module;
            IntPtr hinstance = Marshal.GetHINSTANCE(mainModule);

            SwapchainSource win32Source = SwapchainSource.CreateWin32(Hwnd, hinstance);
            SwapchainDescription scDesc = new SwapchainDescription(win32Source, size.Value.width, size.Value.height, PixelFormat.R32_Float, true);

            var options = new GraphicsDeviceOptions(
              debug: false,
              swapchainDepthFormat: PixelFormat.R16_UNorm,
              syncToVerticalBlank: true,
              resourceBindingModel: ResourceBindingModel.Improved,
              preferDepthRangeZeroToOne: true,
              preferStandardClipSpaceYDirection: true);

            var d3dOptions = new D3D11DeviceOptions()
            {
                //AdapterPtr = GetHardwareAdapter().NativePointer // can only be used with custom version of veldrid where the D3D11GraphicsDevice uses DriverType.Unknown
            };

            _gd = GraphicsDevice.CreateD3D11(options, d3dOptions, scDesc);

            _sc = _gd.ResourceFactory.CreateSwapchain(scDesc);

            graphicsDevice.OnNext(_gd);
            swapchain.OnNext(_sc);
        }

        protected virtual void DestroySwapchain()
        {
            _sc.Dispose();
        }

        private (uint width, uint height) GetSize()
        {
            double dpiScale = GetDpiScale();
            uint width = (uint)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            uint height = (uint)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));
            width = Math.Max(width, 1);
            height = Math.Max(width, 1);
            return (width, height);
        }


        public void Run()
        {
        }

        protected override void Resized()
        {
        }
    }
}
