using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

namespace VeldridView.UWP
{
    public class VeldridPanel : UserControl, IApplicationWindow
    {
        private GraphicsDevice? _gd;
        private readonly Stopwatch _sw;
        private double _previousSeconds;


        private readonly Subject<float> rendering = new();
        private readonly BehaviorSubject<GraphicsDevice?> graphicsDevice = new(null);
        private readonly BehaviorSubject<Swapchain?> swapchain = new(null);
        private readonly BehaviorSubject<(uint width, uint height)> size;

        private Subject<KeyEvent> keyChange = new Subject<KeyEvent>();
        private Subject<PointerEvent> pointerChange = new Subject<PointerEvent>();
        private SwapChainPanel swapchainPanel;

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
            this.Loaded += VeldridPanel_Loaded;


            this.KeyDown += VeldridPanel_KeyDown;
            this.KeyUp += VeldridPanel_KeyUp;
            this.Tapped += VeldridPanel_Tapped;
            this.Holding += VeldridPanel_Holding;
            this.IsDoubleTapEnabled = true;
            this.DoubleTapped += VeldridPanel_DoubleTapped;
            this.ManipulationStarted += VeldridPanel_ManipulationStarted;
            this.ManipulationDelta += VeldridPanel_ManipulationDelta;

            this.ManipulationCompleted += VeldridPanel_ManipulationCompleted;

            swapchainPanel = new SwapChainPanel();

            this.Content = swapchainPanel;


            this.IsTapEnabled = true;
            this.AllowFocusOnInteraction = true;
            this.ManipulationMode = Windows.UI.Xaml.Input.ManipulationModes.TranslateX | Windows.UI.Xaml.Input.ManipulationModes.TranslateY | Windows.UI.Xaml.Input.ManipulationModes.Scale;

            this.PointerWheelChanged += VeldridPanel_PointerWheelChanged;

            //CoreWindow.GetForCurrentThread().KeyDown += VeldridPanel_KeyDown; ;
            //CoreWindow.GetForCurrentThread().KeyUp += VeldridPanel_KeyUp;
        }

        private void VeldridPanel_PointerWheelChanged(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var absDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;


            pointerChange.OnNext(new PointerEvent(System.Numerics.Vector2.Zero, 1,absDelta / 10f));
        }

        private void VeldridPanel_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
        }

        private void VeldridPanel_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            pointerChange.OnNext(new PointerEvent(new System.Numerics.Vector2((float)e.Delta.Translation.X, (float)e.Delta.Translation.Y), e.Delta.Scale,0));
        }

        private void VeldridPanel_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
        {
        }

        private void VeldridPanel_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {

        }

        private void VeldridPanel_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
        }

        private void VeldridPanel_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
        }

        private void VeldridPanel_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            keyChange.OnNext(new KeyEvent(e.Key.ToString(), true));
        }

        private void VeldridPanel_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            keyChange.OnNext(new KeyEvent(e.Key.ToString(), false));
        }


        private void VeldridPanel_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDevice();
        }

        public void Run()
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            if (_gd == null)
            {
                return;
            }

            var newSize = (width:(uint)RenderSize.Width, height: (uint)RenderSize.Height);
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

        void InitializeDevice()
        {
            var ss = SwapchainSource.CreateUwp(swapchainPanel, DisplayInformation.GetForCurrentView().LogicalDpi);
            var scd = new SwapchainDescription(
                ss,
                size.Value.width,
                size.Value.height,
                PixelFormat.R32_Float,
                false);

            var options = new GraphicsDeviceOptions(
              debug: false,
              swapchainDepthFormat: PixelFormat.R32_Float,
              syncToVerticalBlank: true,
              resourceBindingModel: ResourceBindingModel.Improved,
              preferDepthRangeZeroToOne: true,
              preferStandardClipSpaceYDirection: true);
            var backend = GraphicsBackend.Direct3D11;

            var d3dOptions = new D3D11DeviceOptions() 
            {
                DeviceCreationFlags= (uint) Vortice.Direct3D11.DeviceCreationFlags.Debug
            };

            if (backend == GraphicsBackend.Direct3D11)
            {
                _gd = GraphicsDevice.CreateD3D11(options, d3dOptions, scd);
            }
            else
            {
                throw new NotImplementedException();
            }
            graphicsDevice.OnNext(_gd);
            swapchain.OnNext(_gd.MainSwapchain);

        }
    }
}
