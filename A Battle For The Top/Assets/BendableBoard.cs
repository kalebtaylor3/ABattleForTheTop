using UnityEngine;

public class DirectionalBendableBoard : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;

    public float bendStrength = 1f; // Controls the overall bend strength
    public int hingePoints = 5; // Number of hinge points along the length
    public AnimationCurve bendingCurve; // Custom curve to control the bending shape
    public Vector3 bendDirection = Vector3.up; // Direction of the bend

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        modifiedVertices = new Vector3[originalVertices.Length];

        // Normalize the bend direction to ensure consistent behavior
        bendDirection.Normalize();

        // Initialize the bending curve if not assigned
        if (bendingCurve == null)
        {
            bendingCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        }
    }

    void Update()
    {
        ApplyBend();
    }

    void ApplyBend()
    {
        float boardLength = GetBoardLength(); // Calculate the length of the board
        float hingeSpacing = boardLength / hingePoints; // Distance between hinge points

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];
            Vector3 cumulativeBend = Vector3.zero;

            // Apply bending at each hinge point
            for (int hinge = 1; hinge <= hingePoints; hinge++)
            {
                float hingePosition = hinge * hingeSpacing;
                if (vertex.z > hingePosition)
                {
                    // Calculate bend amount using a custom curve and cumulative effect
                    float relativePosition = (vertex.z - hingePosition) / boardLength;
                    float bendAmount = bendingCurve.Evaluate(relativePosition) * bendStrength / hingePoints;

                    // Cumulative effect of bending in the specified direction
                    cumulativeBend += bendDirection * bendAmount;
                }
            }

            // Apply the cumulative bending to the vertex
            vertex += cumulativeBend;
            modifiedVertices[i] = vertex;
        }

        // Update the mesh with the modified vertices
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    float GetBoardLength()
    {
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (Vector3 vertex in originalVertices)
        {
            if (vertex.z < minZ) minZ = vertex.z;
            if (vertex.z > maxZ) maxZ = vertex.z;
        }

        return maxZ - minZ;
    }
}
