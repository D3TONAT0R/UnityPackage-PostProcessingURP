using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.PostProcessing.RenderGraph
{
	[System.Serializable]
	public class ShapeInfo
	{
		public Vector2 position;
		public float size;
		public float sides;
		public Color color;
	}

	public class RGTest : ScriptableRendererFeature
	{
		private RGTestPass pass;

		public ShapeInfo[] shapes;

		public override void Create()
		{
			pass = new RGTestPass();
			pass.renderPassEvent = RenderPassEvent.AfterRendering;
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			pass.shapes = shapes;
			renderer.EnqueuePass(pass);
		}
	}

	public class RGTestPass : ScriptableRenderPass
	{
		public class RGPostProcessingData
		{
			public TextureHandle source;
			public TextureHandle destination;
			public Material material;
			public Vector2 shapePosition;
			public float shapeSize;
			public float shapeSides;
			public Color shapeColor;
		}

		private bool initialized = false;
		private Material blitMaterial;
		private ProfilingSampler sampler;

		public ShapeInfo[] shapes;

		private void Initialize()
		{
			blitMaterial = new Material(Shader.Find("Hidden/RGPostProcessTest"));
			sampler = new ProfilingSampler("RG Post Processing Pass");
			initialized = true;
		}

		public override void RecordRenderGraph(RenderGraphModule.RenderGraph renderGraph, ContextContainer context)
		{
			if(!initialized)
			{
				Initialize();
			}
			UniversalResourceData frameData = context.Get<UniversalResourceData>();
			if(shapes == null || shapes.Length == 0)
				return;
			//renderGraph.BeginProfilingSampler(sampler);
			for(var i = 0; i < shapes.Length; i++)
			{
				var shape = shapes[i];
				DrawShape("Shape " + i, renderGraph, frameData, blitMaterial, shape);
			}
			//renderGraph.EndProfilingSampler(sampler);
		}

		static void DrawShape(string name, RenderGraphModule.RenderGraph renderGraph, UniversalResourceData frameData, Material mat, ShapeInfo shape)
		{
			using(var builder = renderGraph.AddRasterRenderPass<RGPostProcessingData>(name, out var passData))
			{
				passData.source = frameData.activeColorTexture;
				passData.material = mat;

				passData.shapePosition = shape.position;
				passData.shapeSize = shape.size;
				passData.shapeSides = shape.sides;
				passData.shapeColor = shape.color;

				builder.UseTexture(passData.source);

				TextureDesc desc = frameData.activeColorTexture.GetDescriptor(renderGraph);
				desc.depthBufferBits = 0;
				TextureHandle destination = renderGraph.CreateTexture(desc);
				passData.destination = destination;

				builder.SetRenderAttachment(destination, 0);
				builder.AllowGlobalStateModification(true);

				builder.SetRenderFunc<RGPostProcessingData>(Execute);

				frameData.cameraColor = destination;
			}
		}

		static void Execute(RGPostProcessingData data, RasterGraphContext context)
		{
			context.cmd.SetGlobalVector("_ShapeParams", new Vector4(data.shapePosition.x, data.shapePosition.y, data.shapeSize, data.shapeSides));
			context.cmd.SetGlobalColor("_Color", data.shapeColor);
			Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
		}
	}
}
