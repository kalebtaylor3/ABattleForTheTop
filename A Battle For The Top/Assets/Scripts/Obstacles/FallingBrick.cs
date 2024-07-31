using BFTT.Components;
using UnityEngine;

public class FallingBrick : MonoBehaviour
{
    public float lifeTime = 5f; // The time a brick stays active before returning to the pool

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            RigidbodyMover playerMover = collision.gameObject.GetComponent<RigidbodyMover>();
            playerMover.SetVelocity(Vector3.zero);
            Debug.Log("Stopped Player");
        }
    }

    private void OnEnable()
    {
        Invoke("ReturnToPool", lifeTime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void ReturnToPool()
    {
        BrickPool.Instance.ReturnToPool(gameObject);
    }
}
