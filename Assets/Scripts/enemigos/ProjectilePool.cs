// ProjectilePool.cs
using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [Header("Pool Settings")]
    public GameObject projectilePrefab;
    public int poolSize = 50;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject proj = Instantiate(projectilePrefab);
            proj.SetActive(false);
            pool.Enqueue(proj);
        }
    }

    public GameObject GetProjectile()
    {
        if (pool.Count > 0)
        {
            GameObject proj = pool.Dequeue();
            return proj;
        }
        else
        {
            // Expand pool if needed
            GameObject proj = Instantiate(projectilePrefab);
            proj.SetActive(false);
            return proj;
        }
    }

    public void ReturnProjectile(GameObject proj)
    {
        proj.SetActive(false);
        pool.Enqueue(proj);
    }
}
