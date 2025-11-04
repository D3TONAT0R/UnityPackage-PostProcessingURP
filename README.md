# PostProcessing for URP

Provides a custom render feature and volume component classes streamlining the creation of custom post processing effects in the Universal Render Pipeline similar to the Post Processing Stack from the built-in render pipeline.

> [!WARNING]
> This package is currently in development.
 
## Installation

> [!WARNING]
> It is highly recommended to target a specific release when installing to avoid breaking changes when unity reinstalls the package. This can be done by appending '#(tag-name)' at the end of the repo URL, such as '#1.0.0'.
> A list of currently available tags can be found [here](https://github.com/D3TONAT0R/UnityPackage-PostProcessingURP/tags).

### Option 1: Unity Package Manager (recommended)

Open the Package Manager window, click on "Add Package from Git URL ...", then enter the following:
```
https://github.com/d3tonat0r/unitypackage-postProcessingurp.git
```

### Option 2: Manually Editing packages.json

Add the following line to your project's `Packages/manifest.json`:

```json
"com.github.d3tonat0r.postprocessingurp": "https://github.com/d3tonat0r/unitypackage-postprocessingurp.git"
```

## Writing custom effects

Custom volume effects are written using the provided `CustomPostProcessVolumeComponent` class.

```cs
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.PostProcessing;

// Set name to show in "Add Override" menu
[VolumeComponentMenu("Post-processing/(Effect Name)")]
public class CustomEffect : CustomPostProcessVolumeComponent
{
	// Volume properties must be defined using VolumeParameter types to support volume blending.
	public ColorParameter color = new ColorParameter(Color.white, false, false, false);
	public FloatParameter intensity = new FloatParameter(1);

	// Name of the shader to use for the effect
	public override string ShaderName => "Hidden/PostProcessing/CustomEffect";

	// Injection point where the effect will be rendered
	public override PostProcessingPassEvent InjectionPoint => PostProcessingPassEvent.AfterPostProcessing;

	// Optional: Override to control when the effect should be rendered.
	public override bool IsActive()
	{
		return base.IsActive() && intensity.value.a > 0;
	}

	// Set effect material properties here
	public override void SetMaterialProperties(Material material)
	{
		material.SetColor("_Color", color.value);
		material.SetFloat("_Intensity", intensity.value);
	}

	// Optional: Override to perform additional work other than a normal blit operation.
	protected override void RenderEffect(CustomPostProcessPass pass, RenderGraph renderGraph,
		UniversalResourceData frameData, ContextContainer context)
	{
		// Blit twice using two material passes
		Blit(renderGraph, frameData, 0);
		Blit(renderGraph, frameData, 1);
	}
}
```