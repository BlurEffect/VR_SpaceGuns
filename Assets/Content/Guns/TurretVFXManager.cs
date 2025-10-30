using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

// Store all tracer data for one frame in structs like this and then process them all in the Vfx graph 
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct TracerData
{
    public Vector3 position;
    public Vector3 direction;
    public float lifetime;
    public Color color;
    public int id;
}


[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct ProjectileImpactData
{
    public Vector3 position;
    public Vector3 direction;
    public Color color;
}

/*
 *
 * This setup is extremely efficient:

Only one event per frame (minimal CPU overhead).

One GPU buffer upload per frame (linear memory copy).

The GPU handles all the spawning logic internally.

You can easily support thousands of tracers per frame.
 */

public class TurretVFXManager : MonoBehaviour
{
    [SerializeField] private VisualEffect tracerVFX; // assign the single shared VisualEffect component
    [SerializeField] private VisualEffect impactVFX; // assign the single shared VisualEffect component
    
    [SerializeField] private int maxProjectiles = 1024; // assign the single shared VisualEffect component


    private List<TracerData> shots = new();
    private GraphicsBuffer shotBuffer;
    
    private List<ProjectileImpactData> impacts = new();
    private GraphicsBuffer impactBuffer;

    private List<uint> hits = new();
    private GraphicsBuffer hitBuffer;

    private bool hitsDirty;
    
    public static TurretVFXManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        // Init hit list to all false
        for (int i = 0; i < maxProjectiles; ++i)
        {
            hits.Add(0);
        }
        
        int stride = Marshal.SizeOf(typeof(TracerData));
        shotBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1024, stride); // Up to 1024 shots per frame
        tracerVFX.SetGraphicsBuffer("TracerBuffer", shotBuffer);
        
        stride = Marshal.SizeOf(typeof(ProjectileImpactData));
        impactBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1024, stride); // Up to 1024 shots per frame
        impactVFX.SetGraphicsBuffer("ImpactBuffer", impactBuffer);
        
        stride = Marshal.SizeOf(typeof(uint));
        hitBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1024, stride); // Up to 1024 shots per frame
        tracerVFX.SetGraphicsBuffer("HitBuffer", hitBuffer);
    }

    public void DeleteProjectile(int id)
    {
        hits[id] = 1;
        hitsDirty = true;
    }
    
    public void SpawnProjectileImpact(Vector3 position, Vector3 direction, Color color)
    {
        if (impactVFX == null)
        {
            return;
        }
        
        impacts.Add(new ProjectileImpactData { position = position, direction = direction, color = color });
    }
    
    
    // start = muzzle position, end = hit point, speed = tracer speed (m/s)
    public void SpawnTracer(int id, Vector3 start, Vector3 direction, float speed, Color color)
    {
        if (tracerVFX == null)
        {
            return;
        }

        Vector3 dir = direction;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return;

        dir /= dist;
        float lifetime = dist / Mathf.Max(speed, 0.0001f);

        // reset hit info for the particle
        if (hits[id] == 1)
        {
            hits[id] = 0;
        }
        
        shots.Add(new TracerData { id = id, position = start, direction = dir * speed, lifetime = lifetime, color = color });

        return;
        
        
        // Old stuff below
        var vfx = GetComponent<VisualEffect>();

        vfx.SetVector3("StartPosition", start);
        vfx.SetVector3("Direction", dir);
        vfx.SetFloat("Lifetime", 0.3f);

        
        
        vfx.SendEvent("SpawnTracer");
        
        /*
        // Create a VFXEventAttribute from the VisualEffect instance (do NOT use new)
        using (var evt = tracerVFX.CreateVFXEventAttribute())
        {
            // The property names must match those exposed in your VFX Graph exactly.
            // Example property names: "position", "direction", "lifetime" — check your graph!
            evt.SetVector3("Position", start);
            evt.SetVector3("Direction", dir);
            evt.SetFloat("Lifetime", lifetime);
            evt.SetFloat("Distance", dist); // optional, if your graph needs it

            // Send the named event (must match the event name in the VFX Graph)
            tracerVFX.SendEvent("SpawnTracer", evt);
        }
        */
    }
    
    void LateUpdate()
    {
        if (shots.Count > 0)
        {
            // Upload all shots to the GPU
            shotBuffer.SetData(shots);
            tracerVFX.SetInt("TracerCount", shots.Count);

            // Fire a single event to tell the graph to spawn them
            tracerVFX.SendEvent("SpawnTracers");

            shots.Clear();
        }
        
        if (impacts.Count > 0)
        {
            // Upload all shots to the GPU
            impactBuffer.SetData(impacts);
            impactVFX.SetInt("ImpactCount", impacts.Count);

            // Fire a single event to tell the graph to spawn them
            impactVFX.SendEvent("SpawnProjectileImpacts");

            impacts.Clear();
        }

        if (hitsDirty)
        {
            hitBuffer.SetData(hits);
            //hits.Clear();
            hitsDirty = false;
        }
        
    }
    
    void OnDestroy()
    {
        shotBuffer?.Dispose();
        impactBuffer?.Dispose();
        hitBuffer?.Dispose();
    }
}