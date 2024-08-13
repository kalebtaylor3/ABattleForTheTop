using UnityEngine;
using UnityEditor;

public class ReplaceStandardShader : MonoBehaviour
{
    [MenuItem("Tools/Replace Standard Shader")]
    static void ReplaceMaterial()
    {
        // Load your new URP material
        Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/DefaultURPMaterial.mat");

        // Find all renderers in the project
        Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();

        // Loop through all renderers and replace the material if it uses the "Standard" shader
        foreach (Renderer renderer in renderers)
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i].shader.name == "Standard")
                {
                    renderer.sharedMaterials[i] = newMaterial;
                }
            }
        }

        Debug.Log("Replaced all instances of materials using the Standard shader with DefaultURPMaterial.");
    }
}
