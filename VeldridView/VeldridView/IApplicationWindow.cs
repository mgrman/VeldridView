using System;
using System.Threading.Tasks;
using Veldrid;

namespace VeldridView
{
    public interface IApplicationWindow
    {
        IObservable<float> Rendering { get; }
        IObservable<GraphicsDevice?> GraphicsDevice { get; }
        IObservable<Swapchain?> Swapchain { get; }
        IObservable<(uint width,uint height)> Size { get; }

        IObservable<KeyEvent> KeyChange { get; }
        IObservable<PointerEvent> PointerChange { get; }
    }
}
