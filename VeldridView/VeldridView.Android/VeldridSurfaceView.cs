using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Veldrid;

namespace VeldridView.Android
{
    public class VeldridSurfaceView : SurfaceView, ISurfaceHolderCallback, IApplicationWindow
    {
        private readonly GraphicsBackend _backend;
        protected GraphicsDeviceOptions DeviceOptions { get; }
        private bool _needsResize;
        private bool _surfaceCreated;

        private readonly Stopwatch _sw;
        private double _previousSeconds;

        private Subject<float> rendering = new Subject<float>();
        private BehaviorSubject<GraphicsDevice?> graphicsDevice = new BehaviorSubject<GraphicsDevice?>(null);
        private BehaviorSubject<Swapchain?> swapchain = new BehaviorSubject<Swapchain?>(null);
        private BehaviorSubject<(uint width, uint height)> size;
        private Subject<PointerEvent> pointerChange = new Subject<PointerEvent>();

        private Subject<KeyEvent> keyChange = new Subject<KeyEvent>();

        private readonly ScaleGestureDetector _scaleDetector;

        private float _lastTouchX;
        private float _lastTouchY;
        private static readonly int InvalidPointerId = -1;
        private int _activePointerId;

        IObservable<float> IApplicationWindow.Rendering => rendering;

         IObservable<GraphicsDevice?> IApplicationWindow.GraphicsDevice => graphicsDevice;


         IObservable<Swapchain?> IApplicationWindow.Swapchain => swapchain;

         IObservable<(uint width, uint height)> IApplicationWindow.Size => size;
        IObservable<KeyEvent> IApplicationWindow.KeyChange => keyChange;
        IObservable<PointerEvent> IApplicationWindow.PointerChange => pointerChange;

        public VeldridSurfaceView(Context context, GraphicsBackend backend, GraphicsDeviceOptions deviceOptions) : base(context)
        {
            if (!(backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.OpenGLES))
            {
                throw new NotSupportedException($"{backend} is not supported on Android.");
            }

            _backend = backend;
            DeviceOptions = deviceOptions;
            Holder!.AddCallback(this);
            _sw = Stopwatch.StartNew();
            size = new BehaviorSubject<(uint width, uint height)>(((uint)Width, (uint)Height));
            _scaleDetector = new ScaleGestureDetector(context, new MyScaleListener(this));
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            _scaleDetector.OnTouchEvent(ev);

            MotionEventActions action = ev.Action & MotionEventActions.Mask;
            int pointerIndex;

            switch (action)
            {
                case MotionEventActions.Down:
                    _lastTouchX = ev.GetX();
                    _lastTouchY = ev.GetY();
                    _activePointerId = ev.GetPointerId(0);
                    break;

                case MotionEventActions.Move:
                    pointerIndex = ev.FindPointerIndex(_activePointerId);
                    float x = ev.GetX(pointerIndex);
                    float y = ev.GetY(pointerIndex);
                    if (!_scaleDetector.IsInProgress)
                    {
                        // Only move the ScaleGestureDetector isn't already processing a gesture.
                        float deltaX = x - _lastTouchX;
                        float deltaY = y - _lastTouchY;

                        pointerChange.OnNext(new PointerEvent(new System.Numerics.Vector2(deltaX, deltaY), 1, 0));
                    }

                    _lastTouchX = x;
                    _lastTouchY = y;
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    // This events occur when something cancels the gesture (for example the
                    // activity going in the background) or when the pointer has been lifted up.
                    // We no longer need to keep track of the active pointer.
                    _activePointerId = InvalidPointerId;
                    break;

                case MotionEventActions.PointerUp:
                    // We only want to update the last touch position if the the appropriate pointer
                    // has been lifted off the screen.
                    pointerIndex = (int)(ev.Action & MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
                    int pointerId = ev.GetPointerId(pointerIndex);
                    if (pointerId == _activePointerId)
                    {
                        // This was our active pointer going up. Choose a new
                        // action pointer and adjust accordingly
                        int newPointerIndex = pointerIndex == 0 ? 1 : 0;
                        _lastTouchX = ev.GetX(newPointerIndex);
                        _lastTouchY = ev.GetY(newPointerIndex);
                        _activePointerId = ev.GetPointerId(newPointerIndex);
                    }
                    break;
            }
            return true;
        }
        private void OnScaleFactor(float scaleFactor)
        {
            pointerChange.OnNext(new PointerEvent (Vector2.Zero, scaleFactor, 0));
        }

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, global::Android.Views.KeyEvent e)
        {
            keyChange.OnNext(new KeyEvent(keyCode.ToString(), true));
            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, global::Android.Views.KeyEvent e)
        {
            keyChange.OnNext(new KeyEvent(keyCode.ToString(), false));
            return base.OnKeyUp(keyCode, e);
        }



        public void SurfaceCreated(ISurfaceHolder holder)
        {
            if (_backend == GraphicsBackend.Vulkan)
            {
                GraphicsDevice gd= graphicsDevice.Value ?? GraphicsDevice.CreateVulkan(DeviceOptions);

                Debug.Assert(swapchain.Value == null);
                SwapchainSource ss = SwapchainSource.CreateAndroidSurface(holder.Surface!.Handle, JNIEnv.Handle);
                SwapchainDescription sd = new SwapchainDescription(
                    ss,
                    (uint)Width,
                    (uint)Height,
                    DeviceOptions.SwapchainDepthFormat,
                    DeviceOptions.SyncToVerticalBlank);
                graphicsDevice.OnNext(gd);
                swapchain.OnNext(gd.ResourceFactory.CreateSwapchain(sd));
            }
            else
            {
                Debug.Assert(graphicsDevice.Value == null && swapchain.Value == null);
                SwapchainSource ss = SwapchainSource.CreateAndroidSurface(holder.Surface!.Handle, JNIEnv.Handle);
                SwapchainDescription sd = new SwapchainDescription(
                    ss,
                    (uint)Width,
                    (uint)Height,
                    DeviceOptions.SwapchainDepthFormat,
                    DeviceOptions.SyncToVerticalBlank);
                var gd = GraphicsDevice.CreateOpenGLES(DeviceOptions, sd);
                graphicsDevice.OnNext(gd);
                swapchain.OnNext(gd.MainSwapchain);
            }

            _surfaceCreated = true;
            Task.Factory.StartNew(() => RenderLoop(), TaskCreationOptions.LongRunning);
        }


        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            _surfaceCreated = false;
            if (_backend == GraphicsBackend.Vulkan)
            {
                var oldSwapChain = swapchain.Value;
                swapchain.OnNext(null);
                oldSwapChain?.Dispose();
            }
            else
            {
                var oldGd = graphicsDevice.Value;
                graphicsDevice.OnNext(null);
                swapchain.OnNext(null);
                oldGd?.Dispose();
            }

        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            _needsResize = true;
        }

        private void RenderLoop()
        {
            while (true)
            {
                try
                {
                    if (!_surfaceCreated) {

                        return;
                         }


                    if (_needsResize)
                    {
                        _needsResize = false;
                        swapchain.Value?.Resize((uint)Width, (uint)Height);
                        size.OnNext(((uint)Width, (uint)Height));
                    }

                    if (graphicsDevice.Value != null)
                    {
                        double newSeconds = _sw.Elapsed.TotalSeconds;
                        double deltaSeconds = newSeconds - _previousSeconds;
                        _previousSeconds = newSeconds;
                        rendering.OnNext((float)deltaSeconds);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    throw;
                }
            }
        }


        private class MyScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            private readonly VeldridSurfaceView _view;

            public MyScaleListener(VeldridSurfaceView view)
            {
                _view = view;
            }

            public override bool OnScale(ScaleGestureDetector detector)
            {
                _view.OnScaleFactor(detector.ScaleFactor);

                return true;
            }
        }

    }
}