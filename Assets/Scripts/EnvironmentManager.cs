using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Terrain Generation")]
    public int terrainWidth = 256;
    public int terrainHeight = 256;
    public float terrainScale = 20f;
    public float terrainHeightMultiplier = 20f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed = 12345;
    
    [Header("Object Spawning")]
    public GameObject[] environmentObjects;
    public Transform[] spawnZones;
    public int maxObjects = 100;
    public float minDistanceBetweenObjects = 5f;
    
    [Header("Weather System")]
    public Light directionalLight;
    public AnimationCurve lightIntensityCurve;
    public Gradient fogColorGradient;
    public float dayNightCycleSpeed = 0.1f;
    
    [Header("Particle Effects")]
    public GameObject[] particleEffects;
    public float particleSpawnRate = 5f;
    
    private Terrain terrain;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private float timeOfDay = 0f;
    
    void Start()
    {
        GenerateTerrain();
        SpawnEnvironmentObjects();
        StartCoroutine(WeatherSystem());
        StartCoroutine(SpawnParticleEffects());
    }
    
    void GenerateTerrain()
    {
        // Create terrain programmatically
        GameObject terrainObject = new GameObject("ProceduralTerrain");
        terrainObject.AddComponent<Terrain>();
        terrain = terrainObject.GetComponent<Terrain>();
        
        // Set terrain size
        terrain.terrainData = new TerrainData();
        terrain.terrainData.size = new Vector3(terrainWidth, terrainHeightMultiplier * 2, terrainHeight);
        
        // Generate heightmap
        float[,] heights = new float[terrainWidth, terrainHeight];
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int y = 0; y < terrainHeight; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
            }
        }
        
        terrain.terrainData.SetHeights(0, 0, heights);
        
        // Set terrain material
        terrain.materialTemplate = new Material(Shader.Find("Standard"));
        
        // Add collider
        terrain.gameObject.AddComponent<TerrainCollider>();
    }
    
    float CalculateHeight(int x, int y)
    {
        float height = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;
        
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (float)x / terrainWidth * frequency + octaveOffsets[i].x;
            float sampleY = (float)y / terrainHeight * frequency + octaveOffsets[i].y;
            
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            height += perlinValue * amplitude;
            
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        
        height = height / maxValue;
        height = (height + 1) / 2; // Normalize to 0-1 range
        
        return height;
    }
    
    void SpawnEnvironmentObjects()
    {
        System.Random rand = new System.Random(seed);
        
        for (int i = 0; i < maxObjects; i++)
        {
            if (environmentObjects.Length == 0) continue;
            
            GameObject prefab = environmentObjects[rand.Next(0, environmentObjects.Length)];
            
            Vector3 spawnPosition;
            if (spawnZones.Length > 0)
            {
                Transform zone = spawnZones[rand.Next(0, spawnZones.Length)];
                spawnPosition = new Vector3(
                    zone.position.x + rand.Next(-50, 50),
                    0,
                    zone.position.z + rand.Next(-50, 50)
                );
            }
            else
            {
                spawnPosition = new Vector3(
                    rand.Next(-terrainWidth/2, terrainWidth/2),
                    0,
                    rand.Next(-terrainHeight/2, terrainHeight/2)
                );
            }
            
            // Check distance from other objects
            bool tooClose = false;
            foreach (GameObject obj in spawnedObjects)
            {
                if (Vector3.Distance(spawnPosition, obj.transform.position) < minDistanceBetweenObjects)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose)
            {
                GameObject spawnedObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
                
                // Random rotation and scale for natural look
                spawnedObject.transform.rotation = Quaternion.Euler(0, rand.Next(0, 360), 0);
                float scale = rand.Next(80, 120) / 100f;
                spawnedObject.transform.localScale = Vector3.one * scale;
                
                spawnedObjects.Add(spawnedObject);
            }
        }
    }
    
    IEnumerator WeatherSystem()
    {
        while (true)
        {
            timeOfDay += dayNightCycleSpeed * Time.deltaTime;
            
            // Update light intensity based on time of day
            if (directionalLight != null)
            {
                float lightIntensity = lightIntensityCurve.Evaluate(timeOfDay % 1f);
                directionalLight.intensity = lightIntensity;
                
                // Change light color based on time of day
                if (timeOfDay % 1f < 0.25f || timeOfDay % 1f > 0.75f)
                {
                    // Night time - cooler colors
                    directionalLight.color = Color.Lerp(Color.blue, Color.white, (timeOfDay % 1f) * 4);
                }
                else
                {
                    // Day time - warmer colors
                    directionalLight.color = Color.Lerp(Color.yellow, Color.white, ((timeOfDay % 1f) - 0.25f) * 2);
                }
            }
            
            // Update fog
            RenderSettings.fogColor = fogColorGradient.Evaluate(timeOfDay % 1f);
            
            yield return null;
        }
    }
    
    IEnumerator SpawnParticleEffects()
    {
        while (true)
        {
            if (particleEffects.Length > 0)
            {
                GameObject effectPrefab = particleEffects[Random.Range(0, particleEffects.Length)];
                
                // Spawn at random position in world
                Vector3 spawnPos = new Vector3(
                    Random.Range(-terrainWidth/2, terrainWidth/2),
                    5f,
                    Random.Range(-terrainHeight/2, terrainHeight/2)
                );
                
                Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            }
            
            yield return new WaitForSeconds(particleSpawnRate);
        }
    }
    
    void Update()
    {
        // Complex environment interactions
        HandleEnvironmentalHazards();
        UpdateDynamicLighting();
    }
    
    void HandleEnvironmentalHazards()
    {
        // Complex environmental hazard system
        Collider[] hazards = Physics.OverlapSphere(Camera.main.transform.position, 50f);
        foreach (Collider hazard in hazards)
        {
            if (hazard.CompareTag("Hazard"))
            {
                // Apply environmental effect to nearby objects
                ApplyEnvironmentalEffect(hazard.transform.position);
            }
        }
    }
    
    void ApplyEnvironmentalEffect(Vector3 position)
    {
        // Complex environmental effect calculation
        Collider[] affectedObjects = Physics.OverlapSphere(position, 10f);
        foreach (Collider obj in affectedObjects)
        {
            if (obj.CompareTag("Player"))
            {
                // Apply effect to player
                Debug.Log("Player affected by environmental hazard!");
            }
            else if (obj.CompareTag("Enemy"))
            {
                // Apply effect to enemy
                EnemyAI enemy = obj.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    // Modify enemy behavior based on environment
                    enemy.moveSpeed *= 0.8f; // Slow down in hazardous area
                }
            }
        }
    }
    
    void UpdateDynamicLighting()
    {
        // Complex dynamic lighting system
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light != directionalLight)
            {
                // Update other lights based on day/night cycle
                light.intensity = Mathf.Lerp(light.intensity, 
                    light.intensity * (timeOfDay % 1f > 0.7f || timeOfDay % 1f < 0.3f ? 1.5f : 1f), 
                    0.01f);
            }
        }
    }
}