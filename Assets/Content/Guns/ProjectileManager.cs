using System;
using System.Collections.Generic;
using UnityEngine;


public struct ProjectileData
{
    public Vector3 position;
    public Vector3 direction;
    public float speed;
    public float lifetime;
    public Color color;
    public int projectileID;
        
    public ProjectileData(int projectileID, Vector3 position, Vector3 direction, float speed, float lifetime, Color color)
    {
        this.projectileID = projectileID;
        this.position = position;
        this.direction = direction;
        this.speed = speed;
        this.lifetime = lifetime;   
        this.color = color;
    }
}

public class ProjectileManager : MonoBehaviour
{
    [SerializeField] private LayerMask projectileLayerMask;
    [SerializeField] private int maxProjectiles = 1024; // assign the single shared VisualEffect component

    private List<ProjectileData> _projectiles;
    private GraphicsBuffer _shotBuffer;
    
    private int projectileCount;

    //--------------------------------------- Singleton ----------------------------------------------------
    
    public static ProjectileManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }
    
    //------------------------------------- Initialization -------------------------------------------------
    
    void Start()
    {
        _projectiles = new List<ProjectileData>(1000);
    }
    
    //--------------------------------------- Management ---------------------------------------------------

    public void SpawnProjectile(Vector3 position, Vector3 direction, float speed, Color color, float lifetime)
    {
        TurretVFXManager.Instance.SpawnTracer(projectileCount, position, direction, speed, color);
        _projectiles.Add(new ProjectileData(projectileCount, position, direction, speed, lifetime, color));
        projectileCount = (projectileCount + 1) % maxProjectiles;
    }
    
    //--------------------------------------- Singleton ----------------------------------------------------

    private void FixedUpdate()
    {
        for(int i = _projectiles.Count - 1; i >= 0; --i)
        {
            ProjectileData projectile = _projectiles[i];
            float moveDistance = projectile.speed * Time.fixedDeltaTime;
            RaycastHit hit;
            if (Physics.Raycast(projectile.position, projectile.direction, out hit, moveDistance, projectileLayerMask))
            {
                // Projectile hit something
                Debug.Log("Hit on: " + hit.collider.gameObject.name);
                
                TurretVFXManager.Instance.DeleteProjectile(projectile.projectileID);
                TurretVFXManager.Instance.SpawnProjectileImpact(hit.point, hit.normal, projectile.color);
                _projectiles.RemoveAt(i);
            }
            else
            {
                // Projectile did not hit anything
                
                // Adjust lifetime
                projectile.lifetime -= Time.fixedDeltaTime;
                if (projectile.lifetime <= 0)
                {
                    _projectiles.RemoveAt(i);
                }
                else
                {
                    // Move projectile along and update it
                    projectile.position += projectile.direction * moveDistance;
                    _projectiles[i] = projectile;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
