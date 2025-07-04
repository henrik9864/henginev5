﻿using Engine.Graphics;
using Silk.NET.Vulkan;

namespace Engine
{
	public struct PipelineContainer : IPipelineContainer<PipelineContainer, DefaultPipelineInfo, PipelineContainerLayer>
	{
        struct PushConstant
        {
            public Matrix4x4f model;
        }

        public RenderLayer skyboxLayer;
		public RenderLayer pbrLayer;
		public RenderLayer wireframeLayer;
        public RenderLayer guiLayer;
        public RenderLayer gizmoLayer;
        public RenderLayer gizmoLineLayer;

        public PipelineContainer(RenderLayer skyboxLayer, RenderLayer pbrLayer, RenderLayer wireframeLayer, RenderLayer guiLayer, RenderLayer gizmoLayer, RenderLayer gizmoLineLayer)
        {
            this.skyboxLayer = skyboxLayer;
            this.pbrLayer = pbrLayer;
            this.wireframeLayer = wireframeLayer;
            this.guiLayer = guiLayer;
			this.gizmoLayer = gizmoLayer;
			this.gizmoLineLayer = gizmoLineLayer;
        }

        public static PipelineContainer Create<TDescriptorContainer>(VkContext context, RenderPass compatibleRenderPass, in DefaultPipelineInfo info)
			where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, PipelineContainerLayer>
		{
			var pbrDescriptorLayout = CreatePipelineLayout(context, TDescriptorContainer.GetDescriptorSetLayout(context, PipelineContainerLayer.Pbr));
			var guiDescriptorLayout = CreatePipelineLayout(context, TDescriptorContainer.GetDescriptorSetLayout(context, PipelineContainerLayer.Gui));
			var gizmoDescriptorLayout = CreatePipelineLayout(context, TDescriptorContainer.GetDescriptorSetLayout(context, PipelineContainerLayer.Gizmo));

            var skyboxShader = Shader.FromFiles("Shaders/Skybox/SkyboxVert.spv", "Shaders/Skybox/SkyboxFrag.spv");
			var skyboxPipeline = RenderLayer.CreateSkybox(context, skyboxShader, pbrDescriptorLayout, info.extent, compatibleRenderPass);

            var pbrShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
            var pbrPipeline = RenderLayer.CreatePbr(context, pbrShader, pbrDescriptorLayout, info.extent, compatibleRenderPass);

			var wireframeShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/BlackFrag.spv");
			var wireframePipeline = RenderLayer.CreateWireframe(context, wireframeShader, pbrDescriptorLayout, info.extent, compatibleRenderPass);

            var guiShader = Shader.FromFiles("Shaders/Gui/GuiVert.spv", "Shaders/Gui/GuiFrag.spv");
            var guiPipeline = RenderLayer.CreateGui(context, guiShader, guiDescriptorLayout, info.extent, compatibleRenderPass);

            var gizmoShader = Shader.FromFiles("Shaders/Gizmo/GizmoVert.spv", "Shaders/Gizmo/GizmoFrag.spv");
            var gizmoPipeline = RenderLayer.CreateGizmo(context, gizmoShader, gizmoDescriptorLayout, info.extent, compatibleRenderPass);
            var gizmoLinePipeline = RenderLayer.CreateGizmoLine(context, gizmoShader, gizmoDescriptorLayout, info.extent, compatibleRenderPass);

			return new PipelineContainer(skyboxPipeline, pbrPipeline, wireframePipeline, guiPipeline, gizmoPipeline, gizmoLinePipeline);
		}

		public static void Dispose(VkContext context, ref PipelineContainer self)
		{
            // They share layout
            self.skyboxLayer.Dispose(context, true);
            self.pbrLayer.Dispose(context, false);
            self.wireframeLayer.Dispose(context, false);
            self.guiLayer.Dispose(context, true);
            self.gizmoLayer.Dispose(context, true);
		}

		public static Pipeline Get(PipelineContainerLayer layer, ref PipelineContainer self)
        {
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
                    return self.skyboxLayer.pipeline;
                case PipelineContainerLayer.Pbr:
                    return self.pbrLayer.pipeline;
                case PipelineContainerLayer.Wireframe:
                    return self.wireframeLayer.pipeline;
                case PipelineContainerLayer.Gui:
                    return self.guiLayer.pipeline;
                case PipelineContainerLayer.Gizmo:
                    return self.gizmoLayer.pipeline;
                case PipelineContainerLayer.GizmoLine:
                    return self.gizmoLineLayer.pipeline;
                default:
                    throw new Exception();
            }
		}

        public static PipelineLayout GetLayout(PipelineContainerLayer layer, ref PipelineContainer self)
        {
            switch (layer)
            {
                case PipelineContainerLayer.Skybox:
                    return self.skyboxLayer.layout;
                case PipelineContainerLayer.Pbr:
                    return self.pbrLayer.layout;
                case PipelineContainerLayer.Wireframe:
                    return self.wireframeLayer.layout;
                case PipelineContainerLayer.Gui:
                    return self.guiLayer.layout;
                case PipelineContainerLayer.Gizmo:
                    return self.gizmoLayer.layout;
                case PipelineContainerLayer.GizmoLine:
                    return self.gizmoLineLayer.layout;
                default:
                    throw new Exception();
            }
        }

        static unsafe PipelineLayout CreatePipelineLayout(VkContext context, DescriptorSetLayout descriptorSetLayout)
        {
            PipelineColorBlendAttachmentState colorBlendAttatchment = new();
            colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
            colorBlendAttatchment.BlendEnable = false;
            colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
            colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
            colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
            colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
            colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
            colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

            PushConstantRange pushConstant = new();
            pushConstant.Offset = 0;
            pushConstant.Size = (uint)sizeof(PushConstant);
            pushConstant.StageFlags = ShaderStageFlags.VertexBit;

            PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new();
            pipelineLayoutCreateInfo.SType = StructureType.PipelineLayoutCreateInfo;
            pipelineLayoutCreateInfo.SetLayoutCount = 1;
            pipelineLayoutCreateInfo.PSetLayouts = &descriptorSetLayout;
            //pipelineLayoutCreateInfo.PushConstantRangeCount = 1;
            //pipelineLayoutCreateInfo.PPushConstantRanges = &pushConstant;

            var result = context.vk.CreatePipelineLayout(context.device, pipelineLayoutCreateInfo, null, out PipelineLayout pipelineLayout);
            if (result != Result.Success)
                throw new Exception("Failed to create vkPipelineLayout");

            return pipelineLayout;
        }
	}

    public struct RenderLayer
    {
        public Shader shader;
        public Pipeline pipeline;
        public PipelineLayout layout;

        public RenderLayer(Shader shader, Pipeline pipeline, PipelineLayout layout)
        {
            this.shader = shader;
            this.pipeline = pipeline;
            this.layout = layout;
        }

        public unsafe void Dispose(VkContext context, bool disposeLayout)
        {
            if (disposeLayout)
                context.vk.DestroyPipelineLayout(context.device, layout, null);

			context.vk.DestroyPipeline(context.device, pipeline, null);
		}

        public static RenderLayer CreatePbr(VkContext context, Shader shader, PipelineLayout layout, Extent2D extent, RenderPass compatibleRenderPass)
        {
            var pipeline = new GraphicsPipelineBuilder()
                .WithVertexInput(GetPbrBindingDescription(), GetPbrAttributeDescription())
                .WithInputAssembly()
                .WithViewport(extent)
                .WithRasterization(PolygonMode.Fill, CullModeFlags.BackBit, 1)
                .WithMultisample()
                .WithDepthStencil(true)
                .WithColorBlend()
                .WithDynamicState([DynamicState.Viewport, DynamicState.Scissor])
                .Build(context, layout, compatibleRenderPass, shader);

            return new RenderLayer(shader, pipeline, layout);
        }

        public static RenderLayer CreateSkybox(VkContext context, Shader shader, PipelineLayout layout, Extent2D extent, RenderPass compatibleRenderPass)
        {
			var pipeline = new GraphicsPipelineBuilder()
				.WithVertexInput(GetPbrBindingDescription(), GetPbrAttributeDescription())
				.WithInputAssembly()
				.WithViewport(extent)
				.WithRasterization(PolygonMode.Fill, CullModeFlags.FrontBit, 1)
				.WithMultisample()
				.WithDepthStencil(true)
				.WithColorBlend()
				.WithDynamicState([DynamicState.Viewport, DynamicState.Scissor])
				.Build(context, layout, compatibleRenderPass, shader);

			return new RenderLayer(shader, pipeline, layout);
		}

        public static RenderLayer CreateWireframe(VkContext context, Shader shader, PipelineLayout layout, Extent2D extent, RenderPass compatibleRenderPass)
        {
			var pipeline = new GraphicsPipelineBuilder()
				.WithVertexInput(GetPbrBindingDescription(), GetPbrAttributeDescription())
				.WithInputAssembly()
				.WithViewport(extent)
				.WithRasterization(PolygonMode.Line, CullModeFlags.None, 1)
				.WithMultisample()
				.WithDepthStencil(true)
				.WithColorBlend()
				.WithDynamicState([DynamicState.Viewport, DynamicState.Scissor])
				.Build(context, layout, compatibleRenderPass, shader);

			return new RenderLayer(shader, pipeline, layout);
		}

		public static RenderLayer CreateGui(VkContext context, Shader shader, PipelineLayout layout, Extent2D extent, RenderPass compatibleRenderPass)
		{
			var pipeline = new GraphicsPipelineBuilder()
				.WithVertexInput(GetGuiBindingDescription(), GetGuiAttributeDescription())
				.WithInputAssembly()
				.WithViewport(extent)
				.WithRasterization(PolygonMode.Fill, CullModeFlags.None, 1)
				.WithMultisample()
				.WithDepthStencil(false)
				.WithColorBlend()
				.WithDynamicState([DynamicState.Viewport, DynamicState.Scissor])
				.Build(context, layout, compatibleRenderPass, shader);

			return new RenderLayer(shader, pipeline, layout);
		}

		public static RenderLayer CreateGizmo(VkContext context, Shader shader, PipelineLayout layout, Extent2D extent, RenderPass compatibleRenderPass)
		{
			var pipeline = new GraphicsPipelineBuilder()
				.WithVertexInput(GetGizmoBindingDescription(), GetGizmoAttributeDescription())
				.WithInputAssembly()
				.WithViewport(extent)
				.WithRasterization(PolygonMode.Fill, CullModeFlags.BackBit, 1)
				.WithMultisample()
				.WithDepthStencil(false)
				.WithColorBlend()
				.WithDynamicState([DynamicState.Viewport, DynamicState.Scissor])
				.Build(context, layout, compatibleRenderPass, shader);

			return new RenderLayer(shader, pipeline, layout);
		}

		public static RenderLayer CreateGizmoLine(VkContext context, Shader shader, PipelineLayout layout, Extent2D extent, RenderPass compatibleRenderPass)
		{
			var pipeline = new GraphicsPipelineBuilder()
				.WithVertexInput(GetGizmoBindingDescription(), GetGizmoAttributeDescription())
				.WithInputAssembly()
				.WithViewport(extent)
				.WithRasterization(PolygonMode.Line, CullModeFlags.None, 2)
				.WithMultisample()
				.WithDepthStencil(false)
				.WithColorBlend()
				.WithDynamicState([DynamicState.Viewport, DynamicState.Scissor])
				.Build(context, layout, compatibleRenderPass, shader);

			return new RenderLayer(shader, pipeline, layout);
		}

		static VertexInputBindingDescription GetPbrBindingDescription()
		{
			VertexInputBindingDescription description = new();
			description.Binding = 0;
			description.Stride = Vertex.SizeInBytes;
			description.InputRate = VertexInputRate.Vertex;

			return description;
		}

		static Memory<VertexInputAttributeDescription> GetPbrAttributeDescription()
		{
			Memory<VertexInputAttributeDescription> description = new VertexInputAttributeDescription[4];
			// Vertex Position
			description.Span[0].Binding = 0;
			description.Span[0].Location = 0;
			description.Span[0].Format = Format.R32G32B32Sfloat;
			description.Span[0].Offset = 0;

			// Vertex Normal
			description.Span[1].Binding = 0;
			description.Span[1].Location = 1;
			description.Span[1].Format = Format.R32G32B32Sfloat;
			description.Span[1].Offset = sizeof(float) * 3;

			// Vertex UV
			description.Span[2].Binding = 0;
			description.Span[2].Location = 2;
			description.Span[2].Format = Format.R32G32Sfloat;
			description.Span[2].Offset = sizeof(float) * 3 * 2;

			// Vertex Tangent
			description.Span[3].Binding = 0;
			description.Span[3].Location = 3;
			description.Span[3].Format = Format.R32G32B32Sfloat;
			description.Span[3].Offset = sizeof(float) * 3 * 2 + sizeof(float) * 2;

			return description;
		}

		static VertexInputBindingDescription GetGuiBindingDescription()
		{
			VertexInputBindingDescription description = new();
			description.Binding = 0;
			description.Stride = GuiVertex.SizeInBytes;
			description.InputRate = VertexInputRate.Vertex;

			return description;
		}

		static Memory<VertexInputAttributeDescription> GetGuiAttributeDescription()
		{
			Memory<VertexInputAttributeDescription> description = new VertexInputAttributeDescription[2];
			// Vertex Position
			description.Span[0].Binding = 0;
			description.Span[0].Location = 0;
			description.Span[0].Format = Format.R32G32B32A32Sfloat;
			description.Span[0].Offset = 0;

			// Vertex UV
			description.Span[1].Binding = 0;
			description.Span[1].Location = 1;
			description.Span[1].Format = Format.R32G32Sfloat;
			description.Span[1].Offset = sizeof(float) * 4;

			return description;
		}

		static VertexInputBindingDescription GetGizmoBindingDescription()
		{
			VertexInputBindingDescription description = new();
			description.Binding = 0;
			description.Stride = GizmoVertex.SizeInBytes;
			description.InputRate = VertexInputRate.Vertex;

			return description;
		}

		static Memory<VertexInputAttributeDescription> GetGizmoAttributeDescription()
		{
			Memory<VertexInputAttributeDescription> description = new VertexInputAttributeDescription[2];

			// Vertex Position
			description.Span[0].Binding = 0;
			description.Span[0].Location = 0;
			description.Span[0].Format = Format.R32G32B32Sfloat;
			description.Span[0].Offset = 0;

			// Vertex Normal
			description.Span[1].Binding = 0;
			description.Span[1].Location = 1;
			description.Span[1].Format = Format.R32G32B32Sfloat;
			description.Span[1].Offset = sizeof(float) * 3;

			return description;
		}
	}
}
