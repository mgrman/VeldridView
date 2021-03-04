using MyX3DParser.Generated.Model.Statements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private uint _indexCount;
        private Shader[] _shaders;

        private const string VertexCode = @"
#version 450
layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
void main()
{
    gl_Position = vec4(Position.x,Position.y,0, 1);
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
        public GraphicsDevice GraphicsDevice { get; private set; }

        internal void LoadX3d(X3D x3d)
        {

            var shapes = x3d.ParentContext.ShapeNodes;


            var triCount = shapes.Sum(o => o.Mesh.Indices.Count * o.MyPositions.Count) / 3;
            var quadVertices = new VertexPositionColor[triCount * 3];
            var quadIndices = new ushort[triCount * 3];

            int pointIndex = 0;
            foreach (var item in shapes)
            {
                
                var color = new RgbaFloat(item.Material.diffuseColor.R, item.Material.diffuseColor.G, item.Material.diffuseColor.B, item.Material.diffuseColor.A);
                foreach (var position in item.MyPositions)
                {
                    foreach (var index in item.Mesh.Indices)
                    {
                        var vertex = item.Mesh.Vertices[index];

                        quadIndices[pointIndex] = (ushort)index;
                        quadVertices[pointIndex] = new VertexPositionColor(Vector3.Transform(vertex, position.Matrix)*2f, color);


                        pointIndex++;
                    }
                }
            }

            BufferDescription vbDescription = new BufferDescription(
                (uint)(quadVertices.Length * VertexPositionColor.SizeInBytes),
                BufferUsage.VertexBuffer);
            _vertexBuffer = ResourceFactory.CreateBuffer(vbDescription);
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);

            BufferDescription ibDescription = new BufferDescription(
                (uint)(quadIndices.Length * sizeof(ushort)),
                BufferUsage.IndexBuffer);
            _indexBuffer = ResourceFactory.CreateBuffer(ibDescription);

            _indexCount = (uint)quadIndices.Length;

            GraphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);
        }

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


            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
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
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.Outputs = MainSwapchain.Framebuffer.OutputDescription;

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

        }

        protected virtual void CreateSwapchainResources(ResourceFactory factory) { }

        private void PreDraw(float deltaSeconds)
        {
            _camera.Update(deltaSeconds);
        }

        protected void Draw(float deltaSeconds)
        {
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
                indexCount: _indexCount,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            GraphicsDevice.SwapBuffers(MainSwapchain);

            //_cl.Begin();
            //_cl.SetFramebuffer(MainSwapchain.Framebuffer);
            //_cl.ClearColorTarget(0, new RgbaFloat((float)_rnd.NextDouble(), (float)_rnd.NextDouble(), (float)_rnd.NextDouble(), 1f));
            //_cl.End();
            //GraphicsDevice.SubmitCommands(_cl);
            ////GraphicsDevice.WaitForIdle();
            //GraphicsDevice.SwapBuffers(MainSwapchain);
        }

        protected virtual void HandleWindowResize()
        {
            _camera.WindowResized(Window.Width, Window.Height);
        }


        public Stream OpenEmbeddedAssetStream(string name) => GetType().Assembly.GetManifestResourceStream(name);


        struct VertexPositionColor
        {
            public Vector3 Position; // This is the position, in normalized device coordinates.
            public RgbaFloat Color; // This is the color of the vertex.
            public VertexPositionColor(Vector3 position, RgbaFloat color)
            {
                Position = position;
                Color = color;
            }
            public const int SizeInBytes = sizeof(float) *7;
        }
    }
}
