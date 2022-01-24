using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace VeldridView
{
    public  class SampleApplication
    {
        private CommandList? _cl;
        private  Pipeline? _pipeline;
        private Random _rnd;
        private DeviceBuffer? _vertexBuffer;
        private DeviceBuffer? _indexBuffer;
        private Shader[]? _shaders;

        private const string VertexCode = @"
#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

        private const string FragmentCode = @"
#version 450
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;
void main()
{
    fsout_Color = fsin_Color;
}";

        public IApplicationWindow Window { get; }
        public GraphicsDevice? GraphicsDevice { get; private set; }
        public ResourceFactory? ResourceFactory { get; private set; }
        public Swapchain? MainSwapchain { get; private set; }

        public SampleApplication(IApplicationWindow window)
        {
            Window = window;

            Window.GraphicsDevice.CombineLatest(Window.Swapchain, (gd, sc) => (gd, sc))
                .Subscribe(o => OnGraphicsDeviceChanged(o.gd, o.sc));

            window.Rendering.Subscribe(Draw);
            _rnd = new Random();
        }

        public void OnGraphicsDeviceChanged(GraphicsDevice? gd, Swapchain? sc)
        {
            if (gd != null && sc != null)
            {
                GraphicsDevice = gd;
                ResourceFactory = gd.ResourceFactory;
                MainSwapchain = sc;
                CreateResources(ResourceFactory);
                CreateSwapchainResources(ResourceFactory);
            }
            else
            {
                GraphicsDevice = null;
                ResourceFactory = null;
                MainSwapchain = null;
            }
        }

        protected virtual string GetTitle() => GetType().Name;

        protected void CreateResources(ResourceFactory factory)
        {
            if ( GraphicsDevice == null || MainSwapchain ==null)
            {
                Debug.WriteLine("CreateResources was called with null GraphicsDevice or null MainSwapchain");
                return;
            }

            _cl = factory.CreateCommandList();

            VertexPositionColor[] quadVertices =
            {
                new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
            };
            BufferDescription vbDescription = new BufferDescription(
                4 * VertexPositionColor.SizeInBytes,
                BufferUsage.VertexBuffer);
            _vertexBuffer = factory.CreateBuffer(vbDescription);
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);

            ushort[] quadIndices = { 0, 1, 2, 3 };
            BufferDescription ibDescription = new BufferDescription(
                4 * sizeof(ushort),
                BufferUsage.IndexBuffer);
            _indexBuffer = factory.CreateBuffer(ibDescription);
            GraphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexCode),
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentCode),
                "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            // Create pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.Outputs = MainSwapchain.Framebuffer.OutputDescription;

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

        }

        protected virtual void CreateSwapchainResources(ResourceFactory factory) { }

        protected void Draw(float deltaSeconds)
        {
            if (_cl == null || MainSwapchain == null || GraphicsDevice == null)
            {
                Debug.WriteLine("Trying to draw with null device");
                return;
            }

            // Begin() must be called before commands can be issued.
            _cl.Begin();

            // We want to render directly to the output window.
            _cl.SetFramebuffer(MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat((float)_rnd.NextDouble(), (float)_rnd.NextDouble(), (float)_rnd.NextDouble(), 1f));

            // Set all relevant state to draw our quad.
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _cl.SetPipeline(_pipeline);
            // Issue a Draw command for a single instance with 4 indices.
            _cl.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            GraphicsDevice.SwapBuffers(MainSwapchain);
        }


        public Stream OpenEmbeddedAssetStream(string name) => GetType().Assembly.GetManifestResourceStream(name);


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
