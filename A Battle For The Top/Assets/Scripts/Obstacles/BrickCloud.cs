using System.Collections;
using UnityEngine;

public class BrickCloud : MonoBehaviour
{
    public float spawnInterval = 0.5f; // Adjust as needed
    public Vector3 spawnAreaSize = new Vector3(10f, 1f, 10f);
    public int maxBricksInCloud = 10;

    private void Start()
    {
        StartCoroutine(SpawnBricks());
    }

    private IEnumerator SpawnBricks()
    {
        while (true)
        {
            while (BrickPool.Instance.ActiveCount() < maxBricksInCloud)
            {
                Vector3 spawnPosition = transform.position + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );

                GameObject brick = BrickPool.Instance.GetPooledObject();
                if (brick != null)
                {
                    brick.transform.position = spawnPosition;
                    brick.SetActive(true);
                }

                yield return new WaitForSeconds(spawnInterval);
            }

            yield return null;  // Wait a frame before checking again
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
}
