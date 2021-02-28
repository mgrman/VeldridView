using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace VeldridView
{
    public  class SampleApplication
    {
        protected Camera _camera;
        private CommandList _cl;
        private  Pipeline _pipeline;
        private Random _rnd;

        public IApplicationWindow Window { get; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain MainSwapchain { get; private set; }

        public SampleApplication(IApplicationWindow window)
        {
            Window = window;
            Window.Resized += HandleWindowResize;
            Window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;
            Window.GraphicsDeviceDestroyed += OnDeviceDestroyed;
            Window.Rendering += PreDraw;
            Window.Rendering += Draw;

            _camera = new Camera(Window.Width, Window.Height);
        }

        public void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            GraphicsDevice = gd;
            ResourceFactory = factory;
            MainSwapchain = sc;
            CreateResources(factory);
            CreateSwapchainResources(factory);
        }

        protected virtual void OnDeviceDestroyed()
        {
            GraphicsDevice = null;
            ResourceFactory = null;
            MainSwapchain = null;
        }

        protected virtual string GetTitle() => GetType().Name;

        protected void CreateResources(ResourceFactory factory)
        {
            _cl = factory.CreateCommandList();
            _rnd = new Random();
        }

        protected virtual void CreateSwapchainResources(ResourceFactory factory) { }

        private void PreDraw(float deltaSeconds)
        {
            _camera.Update(deltaSeconds);
        }

        protected void Draw(float deltaSeconds)
        {
            _cl.Begin();
            _cl.SetFramebuffer(MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat((float)_rnd.NextDouble(), (float)_rnd.NextDouble(), (float)_rnd.NextDouble(), 1f));
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.WaitForIdle();
            GraphicsDevice.SwapBuffers(MainSwapchain);

        }

        protected virtual void HandleWindowResize()
        {
            _camera.WindowResized(Window.Width, Window.Height);
        }


        public Stream OpenEmbeddedAssetStream(string name) => GetType().Assembly.GetManifestResourceStream(name);

        public Shader LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
        {
            string name = $"{set}-{stage.ToString().ToLower()}.{GetExtension(factory.BackendType)}";
            return factory.CreateShader(new ShaderDescription(stage, ReadEmbeddedAssetBytes(name), entryPoint));
        }

        public byte[] ReadEmbeddedAssetBytes(string name)
        {
            using (Stream stream = OpenEmbeddedAssetStream(name))
            {
                byte[] bytes = new byte[stream.Length];
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    stream.CopyTo(ms);
                    return bytes;
                }
            }
        }

        private static string GetExtension(GraphicsBackend backendType)
        {
			bool isMacOS = RuntimeInformation.OSDescription.Contains("Darwin");

            return (backendType == GraphicsBackend.Direct3D11)
                ? "hlsl.bytes"
                : (backendType == GraphicsBackend.Vulkan)
                    ? "450.glsl.spv"
                    : (backendType == GraphicsBackend.Metal)
					    ? isMacOS ? "metallib" : "ios.metallib"
                        : (backendType == GraphicsBackend.OpenGL)
                            ? "330.glsl"
                            : "300.glsles";
        }

        struct VertexPositionColor
        {
            public Vector2 Position; // This is the position, in normalized device coordinates.
            public RgbaFloat Color; // This is the color of the vertex.
            public VertexPositionColor(Vector2 position, RgbaFloat color)
            {
                Position = position;
                Color = color;
            }
            public const uint SizeInBytes = 24;
        }
    }
}
