using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameCollisionHandler : MonoBehaviour
{
    private ParticleSystem flameParticles;
    private HashSet<IceMaterial> iceMaterials = new HashSet<IceMaterial>();

    private void Start()
    {
        flameParticles = GetComponent<ParticleSystem>();
        var collisionModule = flameParticles.collision;
        collisionModule.enabled = true;  // Ensure collision is enabled on the ParticleSystem
        collisionModule.type = ParticleSystemCollisionType.World; // Set the type to World
    }

    private void Update()
    {
        if (iceMaterials.Count > 0)
        {
            // Check if any ice material is still being hit by particles
            foreach (var iceMaterial in iceMaterials)
            {
                if (iceMaterial != null)
                {
                    iceMaterial.StopMelting();
                }
            }
            iceMaterials.Clear();
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Ice"))
        {
            IceMaterial iceMaterial = other.GetComponent<IceMaterial>();
            if (iceMaterial != null)
            {
                iceMaterial.StartMelting();
                iceMaterials.Add(iceMaterial);
            }
        }
    }

    private void OnParticleSystemStopped()
    {
        // When the particle system stops, stop melting and start refreezing
        foreach (var iceMaterial in iceMaterials)
        {
            if (iceMaterial != null)
            {
                iceMaterial.StopMelting();
            }
        }
        iceMaterials.Clear();
    }
}
