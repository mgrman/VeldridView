using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace VeldridView.UWP
{
    public class VeldridPanel : SwapChainPanel, IApplicationWindow
    {
        private GraphicsDevice _gd;

        public uint Width => (uint)this.ActualWidth;

        public uint Height => (uint)this.ActualHeight;

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
            Rendering?.Invoke(0.1f);
        }

        void InitializeDevice()
        {
            var ss = SwapchainSource.CreateUwp(this as SwapChainPanel, DisplayInformation.GetForCurrentView().LogicalDpi);
            var scd = new SwapchainDescription(
                ss,
                (uint)1000,
                (uint)1000,
                PixelFormat.R32_Float,
                false);

            var debug = false;
#if DEBUG
            debug = true;
#endif

            var options = new GraphicsDeviceOptions(
                debug,
                PixelFormat.R16_UNorm,
                false,
                ResourceBindingModel.Improved,
                true,
                true);
            var backend = GraphicsBackend.Direct3D11;

            var d3dOptions = new D3D11DeviceOptions();

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
    }
}
