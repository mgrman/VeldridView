using System;
using System.Collections.Generic;
using System.Linq;
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
        private event Action _resized;

        // set to 1000 since during initialization of device in VeldridPanel_Loaded the size is not yet known.
        public uint Width { get; private set; } = 1000;
        public uint Height { get; private set; } = 1000;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        event Action IApplicationWindow.Resized { add { _resized += value; }remove { _resized -= value; } }

        public bool IsRendering { get; private set; }


        public VeldridPanel()
        {
        }

        protected override sealed void Initialize()
        {
            CreateSwapchain();

            IsRendering = true;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        protected override sealed void Uninitialize()
        {
            IsRendering = false;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;

            DestroySwapchain();
        }

        protected sealed override void Resized()
        {
            ResizeSwapchain();
        }



        private void OnCompositionTargetRendering(object sender, EventArgs eventArgs)
        {
            if (!IsRendering)
                return;

            Rendering?.Invoke(0.1f);
        }

        private double GetDpiScale()
        {
            PresentationSource source = PresentationSource.FromVisual(this);

            return source.CompositionTarget.TransformToDevice.M11;
        }


        protected virtual void CreateSwapchain()
        {
            double dpiScale = GetDpiScale();
            uint width = (uint)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            uint height = (uint)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));

            Module mainModule = typeof(VeldridPanel).Module;
            IntPtr hinstance = Marshal.GetHINSTANCE(mainModule);

            SwapchainSource win32Source = SwapchainSource.CreateWin32(Hwnd, hinstance);
            SwapchainDescription scDesc = new SwapchainDescription(win32Source, 100,100, PixelFormat.R32_Float, true);

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

            GraphicsDeviceCreated?.Invoke(_gd, _gd.ResourceFactory, _sc);
        }

        protected virtual void DestroySwapchain()
        {
            _sc.Dispose();
        }

        private void ResizeSwapchain()
        {
            double dpiScale = GetDpiScale();
            uint width = (uint)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            uint height = (uint)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));
            width = Math.Max(width, 1);
            height = Math.Max(width, 1);
            _sc.Resize(width, height);

            _resized?.Invoke();
        }


        public void Run()
        {
        }
    }
}
