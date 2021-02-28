using CoreAnimation;
using Foundation;
using System;
using UIKit;
using Veldrid;

namespace VeldridView.iOS
{
    public class UIViewApplicationWindow : UIView, IApplicationWindow
    {
        private readonly GraphicsDeviceOptions _options;
        private readonly GraphicsBackend _backend;
        private GraphicsDevice _gd;
        private CADisplayLink _timer;
        private Swapchain _sc;
        private bool _viewLoaded;

        public UIViewApplicationWindow() : base()
        {
            _backend = GraphicsBackend.Metal;
            _options = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved);

            ViewDidLoad();
        }

        public uint Width => (uint)Frame.Width;
        public uint Height => (uint)Frame.Height;


        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;

        public void Run()
        {
            _timer = CADisplayLink.Create(Render);
            _timer.FrameInterval = 1;
            _timer.AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void Render()
        {
            if (_viewLoaded)
            {
                float elapsed = (float)(_timer.TargetTimestamp - _timer.Timestamp);
                Rendering?.Invoke(elapsed);
            }
        }


        private  void ViewDidLoad()
        {
            SwapchainSource ss = SwapchainSource.CreateUIView(this.Handle);
            SwapchainDescription scd = new SwapchainDescription(
                ss,
                (uint)Frame.Width,
                (uint)Frame.Height,
                PixelFormat.R32_Float,
                false);
            if (_backend == GraphicsBackend.Metal)
            {
                _gd = GraphicsDevice.CreateMetal(_options);
                _sc = _gd.ResourceFactory.CreateSwapchain(ref scd);
            }
            else if (_backend == GraphicsBackend.OpenGLES)
            {
                _gd = GraphicsDevice.CreateOpenGLES(_options, scd);
                _sc = _gd.MainSwapchain;
            }
            else if (_backend == GraphicsBackend.Vulkan)
            {
                throw new NotImplementedException();
            }

            GraphicsDeviceCreated?.Invoke(_gd, _gd.ResourceFactory, _sc);
            _viewLoaded = true;
        }

        // Called whenever view changes orientation or layout is changed
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            _sc.Resize((uint)Frame.Width, (uint)Frame.Height);
            Resized?.Invoke();
        }
    }
}