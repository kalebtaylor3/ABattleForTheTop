using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBarObsticle : MonoBehaviour
{

    public BoxCollider[] colliders;

    private void OnEnable()
    {
        SpinningObstacle.OnCollision += DisableCollisions;
    }

    private void OnDisable()
    {
        SpinningObstacle.OnCollision -= DisableCollisions;
    }

    void DisableCollisions()
    {
        StopAllCoroutines(); 
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
        StartCoroutine(WaitForHit());
    }

    IEnumerator WaitForHit()
    {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }
    }
}
