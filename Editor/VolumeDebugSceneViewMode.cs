using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.PostProcessing
{
	public static class VolumeDebugSceneViewMode
	{
		public static bool IsActive(SceneView sceneView)
		{
			return sceneView.cameraMode.drawMode == DrawCameraMode.UserDefined && sceneView.cameraMode.name == "Post Processing";
		}

		[InitializeOnLoadMethod]
		private static void Init()
		{
			var modes = (List<SceneView.CameraMode>)typeof(SceneView).GetProperty("userDefinedModes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
			SceneView.AddCameraMode("Post Processing", "Rendering");
			modes.Add(new SceneView.CameraMode()
			{
				drawMode = DrawCameraMode.Textured,
				name = "Post Processing",
				section = "Rendering"
			});
			SceneView.beforeSceneGui += BeforeSceneGUI;
		}

		private static void BeforeSceneGUI(SceneView view)
		{
			if(Event.current.type != EventType.Repaint)
				return;
			if(view.cameraMode.drawMode != DrawCameraMode.UserDefined || view.cameraMode.name != "Post Processing")
				return;

			var mode = view.cameraMode;
			mode.drawMode = DrawCameraMode.Textured;
			view.cameraMode = mode;
			view.camera.allowHDR = true;
		}
	}
}
