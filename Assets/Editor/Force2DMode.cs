using UnityEditor;

// Makes this a 2D project: new textures import as sprites and the Scene view opens in 2D.
[InitializeOnLoad]
public static class Force2DMode
{
    static Force2DMode()
    {
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        EditorApplication.delayCall += () =>
        {
            foreach (SceneView view in SceneView.sceneViews)
                view.in2DMode = true;
        };
    }
}
