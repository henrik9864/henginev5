﻿using EnCS;
using ImageLib;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Buffers;
using System.Runtime.InteropServices;
using Image = Silk.NET.Vulkan.Image;

namespace Engine
{
	public class VkRenderContext
	{
		VkContext context;

		public CommandPool commandPool;
		public FixedArray16<Sampler> samplers;

		public RenderPipeline<SwapchainRenderTargetManager, DefaultRenderPassInfo, DefaultPipelineInfo, DescriptorSetContainer, PipelineContainer, PipelineContainerLayer, RenderPassContainer, RenderPassId> pipeline;
		//public RenderPipeline<TextureRenderTargetManager<PbrDescriptorSet, Rgba32>, DefaultRenderPassInfo, DefaultPipelineInfo, PbrDescriptorSet, PbrPipelineContainer, PbrPipelineLayer, RenderPassContainer, PbrRenderPassId> texturePipeline;

        public VkRenderContext(VkContext context)
        {
			this.context = context;
        }

		Image image;
		DeviceMemory imageMemory;
		ImageView imageView;

		Image image2;
		DeviceMemory imageMemory2;
		ImageView imageView2;

		Image image3;
		DeviceMemory imageMemory3;
		ImageView imageView3;

		public void Setup()
		{
			samplers = new FixedArray16<Sampler>();
			for (int i = 0; i < 16; i++)
			{
				samplers[i] = VulkanHelper.CreateSampler(context, 0);
			}

			samplers[8] = VulkanHelper.CreateSampler(context, 5);

			uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);

			//surface = CreateSurface(context);
			commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

			//renderPipeline = RenderPipeline.Create(context, surface, commandPool);

			//SwapchainRenderTargetManager<DefaultDescriptorSet> renderTargetManager = SwapchainRenderTargetManager<DefaultDescriptorSet>.Create(context, commandPool);
            pipeline = RenderPipeline<SwapchainRenderTargetManager, DefaultRenderPassInfo, DefaultPipelineInfo, DescriptorSetContainer, PipelineContainer, PipelineContainerLayer, RenderPassContainer, RenderPassId>.Create(context, commandPool);

			/*
			Extent2D extent = swapchain.GetExtent();

            image = VulkanHelper.CreateImage(context, new Extent3D(extent.Width, extent.Height, 1), ImageType.Type2D, Format.B8G8R8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.ColorAttachmentBit, ImageCreateFlags.None, 1, 1);
            imageMemory = VulkanHelper.CreateMemory(context, image, MemoryPropertyFlags.DeviceLocalBit);
            imageView = VulkanHelper.CreateImageView(context, image, ImageViewType.Type2D, Format.B8G8R8A8Srgb, ImageAspectFlags.ColorBit, 1);

            image2 = VulkanHelper.CreateImage(context, new Extent3D(extent.Width, extent.Height, 1), ImageType.Type2D, Swapchain.GetDepthFormat(context), ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, ImageCreateFlags.None, 1, 1);
            imageMemory2 = VulkanHelper.CreateMemory(context, image2, MemoryPropertyFlags.DeviceLocalBit);
            imageView2 = VulkanHelper.CreateImageView(context, image2, ImageViewType.Type2D, Swapchain.GetDepthFormat(context), ImageAspectFlags.DepthBit, 1);

            image3 = VulkanHelper.CreateImage(context, new Extent3D(extent.Width, extent.Height, 1), ImageType.Type2D, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.ColorAttachmentBit, ImageCreateFlags.None, 1, 1);
            imageMemory3 = VulkanHelper.CreateMemory(context, image3, MemoryPropertyFlags.DeviceLocalBit);
            imageView3 = VulkanHelper.CreateImageView(context, image3, ImageViewType.Type2D, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit, 1);

            FixedArray2<Image> images = new FixedArray2<Image>();
            images[0] = image;
            images[1] = image2;

			FixedArray2<ImageView> imageViews = new FixedArray2<ImageView>();
			imageViews[0] = imageView;
			imageViews[1] = imageView2;

            FixedArray2<Format> formats = new FixedArray2<Format>();
            formats[0] = Format.B8G8R8A8Srgb;
            formats[1] = Swapchain.GetDepthFormat(context);

            TextureRenderTargetManager<PbrDescriptorSet, Rgba32> textureTargetManager = TextureRenderTargetManager<PbrDescriptorSet, Rgba32>.Create(SaveImage, context, extent, images, imageViews, formats, container.skyboxRenderPass, commandPool);
			texturePipeline = RenderPipeline<TextureRenderTargetManager<PbrDescriptorSet, Rgba32>, DefaultRenderPassInfo, DefaultPipelineInfo, PbrDescriptorSet, PbrPipelineContainer, PbrPipelineLayer, RenderPassContainer, PbrRenderPassId>.Create(context, textureTargetManager);
			*/
        }

		/*
		static void SaveImage(ImageMemory<Rgba32> img)
		{
            img.ProcessPixelRows(a => {
                for (int y = 0; y < a.Height; y++)
                {
                    Span<Rgba32> row = a.GetRowSpan(y);

                    for (int x = 0; x < row.Length; x++)
                    {
                        ref Rgba32 pixel = ref row[x];
                        byte r = pixel.R;
                        byte g = pixel.G;
                        byte b = pixel.B;
                        pixel = new Rgba32(b, g, r, pixel.A);
                    }
                }
            });

            //img.Save("test.jpg", new ImageEncoder());
        }
		*/
    }

	public class VkContext
	{
		static string[] DEVICE_EXTENSIONS =
		[
			KhrSwapchain.ExtensionName,
			//KhrSurface.ExtensionName,
			KhrPushDescriptor.ExtensionName,
		];

		public Vk vk;
		public IWindow window;

		public Instance instance;
		public PhysicalDevice physicalDevice;
		public Device device;

		public KhrSurface surface;
		public KhrSwapchain swapchain;

		public VkContext(Vk vk, IWindow window)
		{
			this.vk = vk;
			this.window = window;
		}

		public void Setup(EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			instance = CreateInstance(vk, window, vulkanConfig.validationLayers, engineConfig);
			physicalDevice = PickPhysicalDevice(vk, instance);
			var graphicsQueueFamily = GetGraphicsQueueFamily(vk, physicalDevice);
			device = CreateLogicalDevice(vk, physicalDevice, graphicsQueueFamily, vulkanConfig.validationLayers, DEVICE_EXTENSIONS);

			//vk.GetPhysicalDeviceProperties(physicalDevice, out PhysicalDeviceProperties properties);
			//Console.WriteLine(properties.Limits.MinUniformBufferOffsetAlignment);

            // Do not use TryGetInstanceExtension or TryGetDeviceExtensions since they use reflection.
            surface = new KhrSurface(new LamdaNativeContext((string x) => (nint)vk.GetInstanceProcAddr(instance, x)));
			swapchain = new KhrSwapchain(new LamdaNativeContext((string x) => (nint)vk.GetDeviceProcAddr(device, x)));
		}

		static unsafe Instance CreateInstance(Vk vk, IWindow window, string[] validationLayers, EngineConfig engineConfig)
		{
			ApplicationInfo appInfo = new();
			appInfo.SType = StructureType.ApplicationInfo;
			appInfo.PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(engineConfig.appName);
			appInfo.ApplicationVersion = new Version32((uint)engineConfig.appVersion.Major, (uint)engineConfig.appVersion.Minor, (uint)engineConfig.appVersion.Build);
			appInfo.PEngineName = (byte*)Marshal.StringToHGlobalAnsi(engineConfig.engineName);
			appInfo.EngineVersion = new Version32((uint)engineConfig.engineVersion.Major, (uint)engineConfig.engineVersion.Minor, (uint)engineConfig.engineVersion.Build);
			appInfo.ApiVersion = Vk.Version13;

			InstanceCreateInfo createInfo = new();
			createInfo.SType = StructureType.InstanceCreateInfo;
			createInfo.PApplicationInfo = &appInfo;

			var glfwExtensions = window.VkSurface.GetRequiredExtensions(out var count);
			createInfo.EnabledExtensionCount = count;
			createInfo.PpEnabledExtensionNames = glfwExtensions;

			/*
			string[] strArr = new string[count];
			SilkMarshal.CopyPtrToStringArray((nint)glfwExtensions, strArr);
            Console.WriteLine("Instance Extensions:");
            for (int i = 0; i < count; i++)
			{
				Console.WriteLine($"\t{strArr[i]}");
            }
			*/

			if (validationLayers.Length > 0)
			{
				createInfo.EnabledLayerCount = (uint)validationLayers.Length;
				createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			var result = vk.CreateInstance(in createInfo, null, out Instance instance);
			if (result != Result.Success)
				throw new Exception($"Failed to create vkInstance, {result}");

			Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
			Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);

			return instance;
		}

		static unsafe Device CreateLogicalDevice(Vk vk, PhysicalDevice physicalDevice, uint queueFamily, string[] validationLayers, string[] extensions)
		{
			float queuePriority = 1f;

			DeviceQueueCreateInfo queueCreateInfo = new();
			queueCreateInfo.SType = StructureType.DeviceQueueCreateInfo;
			queueCreateInfo.QueueFamilyIndex = queueFamily;
			queueCreateInfo.QueueCount = 1;
			queueCreateInfo.PQueuePriorities = &queuePriority;

			PhysicalDeviceFeatures deviceFeatures = new();
			deviceFeatures.SamplerAnisotropy = true;
			deviceFeatures.FillModeNonSolid = true;
			deviceFeatures.WideLines = true;

			DeviceCreateInfo createInfo = new();
			createInfo.SType = StructureType.DeviceCreateInfo;
			createInfo.PQueueCreateInfos = &queueCreateInfo;
			createInfo.QueueCreateInfoCount = 1;
			createInfo.PEnabledFeatures = &deviceFeatures;

			if (validationLayers.Length > 0)
			{
				createInfo.EnabledLayerCount = (uint)validationLayers.Length;
				createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			if (extensions.Length > 0)
			{
				createInfo.EnabledExtensionCount = (uint)extensions.Length;
				createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			var result = vk.CreateDevice(physicalDevice, in createInfo, null, out Device device);
			if (result != Result.Success)
				throw new Exception($"Failed to create vkDevice, {result}");

			return device;
		}

		static unsafe PhysicalDevice PickPhysicalDevice(Vk vk, Instance instance)
		{
			uint deviceCount = 0;
			vk.EnumeratePhysicalDevices(instance, &deviceCount, null);

			Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)deviceCount];
			vk.EnumeratePhysicalDevices(instance, ref deviceCount, ref devices[0]);

			PhysicalDeviceProperties properties = new();
			PhysicalDeviceFeatures features = new();
			vk.GetPhysicalDeviceProperties(devices[0], &properties);
			vk.GetPhysicalDeviceFeatures(devices[0], &features);

			return devices[0];
		}

		static unsafe uint GetGraphicsQueueFamily(Vk vk, PhysicalDevice physicalDevice)
		{
			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = GetQueueFamilies(vk, qFamilies, physicalDevice);

			for (uint i = 0; i < qFamilies.Length; i++)
			{
				if (qFamilies[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
					return i;
			}

			throw new Exception("Queue with graphics bit not found.");
		}

		static unsafe Span<QueueFamilyProperties> GetQueueFamilies(Vk vk, Span<QueueFamilyProperties> buff, PhysicalDevice physicalDevice)
		{
			uint qFamilyCount = 0;
			vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, null);

			fixed (QueueFamilyProperties* buffPtr = buff)
			{
				vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, buffPtr);
			}

			return buff.Slice(0, (int)qFamilyCount);
		}
	}
}
