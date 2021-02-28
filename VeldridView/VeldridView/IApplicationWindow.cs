using System;
using Veldrid;

namespace VeldridView
{
    public interface IApplicationWindow
    {
        event Action<float> Rendering;
        event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        event Action GraphicsDeviceDestroyed;
        event Action Resized;

        uint Width { get; }
        uint Height { get; }

        void Run();
    }
}
