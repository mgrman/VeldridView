using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace VeldridView.UWP
{
    public class VeldridPanel : SwapChainPanel, IApplicationWindow
    {
        private GraphicsDevice _gd;
        private IDXGIFactory2 Factory;


        // set to 1000 since during initialization of device in VeldridPanel_Loaded the size is not yet known.
        public uint Width { get; private set; } = 1000;
        public uint Height { get; private set; } = 1000;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;

        public VeldridPanel()
        {
            this.Loaded += VeldridPanel_Loaded;
        }

        private void VeldridPanel_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            InitializeDevice();
        }

        public void Run()
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            var newWidth = (uint)RenderSize.Width;
            var newHeight = (uint)RenderSize.Height;
            if (newWidth != Width || newHeight != Height)
            {
                Width = newWidth;
                Height = newHeight;
                _gd.MainSwapchain.Resize(Width, Height);
                Resized?.Invoke();
            }

            Rendering?.Invoke(0.1f);
        }

        void InitializeDevice()
        {
            if (DXGI.CreateDXGIFactory1(out Factory).Failure)
            {
                throw new InvalidOperationException("Cannot create IDXGIFactory1");
            }

            var ss = SwapchainSource.CreateUwp(this as SwapChainPanel, DisplayInformation.GetForCurrentView().LogicalDpi);
            var scd = new SwapchainDescription(
                ss,
                Width,
                Height,
                PixelFormat.R32_Float,
                false);

            var debug = false;
#if DEBUG
            debug = true;
#endif
            var options = new GraphicsDeviceOptions(
              debug: false,
              swapchainDepthFormat: PixelFormat.R16_UNorm,
              syncToVerticalBlank: true,
              resourceBindingModel: ResourceBindingModel.Improved,
              preferDepthRangeZeroToOne: true,
              preferStandardClipSpaceYDirection: true);
            var backend = GraphicsBackend.Direct3D11;

            var d3dOptions = new D3D11DeviceOptions() 
            {
                //AdapterPtr = GetHardwareAdapter().NativePointer // can only be used with custom version of veldrid where the D3D11GraphicsDevice uses DriverType.Unknown
            };

            if (backend == GraphicsBackend.Direct3D11)
            {
                _gd = GraphicsDevice.CreateD3D11(options, d3dOptions, scd);
            }
            else
            {
                throw new NotImplementedException();
            }

            GraphicsDeviceCreated?.Invoke(_gd, _gd.ResourceFactory, _gd.MainSwapchain);

        }


        private IDXGIAdapter1 GetHardwareAdapter()
        {
            IDXGIAdapter1 adapter = null;
            IDXGIFactory6 factory6 = Factory.QueryInterfaceOrNull<IDXGIFactory6>();
            if (factory6 != null)
            {
                for (int adapterIndex = 0;
                    factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out adapter) != Vortice.DXGI.ResultCode.NotFound;
                    adapterIndex++)
                {
                    AdapterDescription1 desc = adapter.Description1;

                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        // Don't select the Basic Render Driver adapter.
                        adapter.Dispose();
                        continue;
                    }

                    return adapter;
                }


                factory6.Dispose();
            }

            if (adapter == null)
            {
                for (int adapterIndex = 0;
                    Factory.EnumAdapters1(adapterIndex, out adapter) != Vortice.DXGI.ResultCode.NotFound;
                    adapterIndex++)
                {
                    AdapterDescription1 desc = adapter.Description1;

                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        // Don't select the Basic Render Driver adapter.
                        adapter.Dispose();
                        continue;
                    }

                    return adapter;
                }
            }

            return adapter;
        }
    }
}
