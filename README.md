# PostProcessing for URP

Provides a custom render feature and volume component classes streamlining the creation of custom post processing effects in the Universal Render Pipeline similar to the Post Processing Stack from the built-in render pipeline.
 
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

- to do