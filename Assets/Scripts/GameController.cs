using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
    public GameObject player;
    public GameObject[] enemies;
    public GameObject[] powerUps;
    public Transform[] spawnPoints;
    public float gameDuration = 60f;
    public float enemySpawnRate = 2f;
    public float powerUpSpawnRate = 10f;
    
    private float timer;
    private int score;
    private bool gameRunning = true;
    
    void Start()
    {
        timer = gameDuration;
        score = 0;
        StartCoroutine(SpawnEnemies());
        StartCoroutine(SpawnPowerUps());
        StartCoroutine(GameTimer());
    }
    
    void Update()
    {
        if (gameRunning)
        {
            timer -= Time.deltaTime;
            
            // Complex player movement with physics
            HandlePlayerMovement();
            
            // Update UI
            UpdateGameUI();
        }
    }
    
    IEnumerator SpawnEnemies()
    {
        while (gameRunning)
        {
            if (spawnPoints.Length > 0)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject enemy = enemies[Random.Range(0, enemies.Length)];
                
                Instantiate(enemy, spawnPoint.position, Quaternion.identity);
            }
            
            yield return new WaitForSeconds(enemySpawnRate);
        }
    }
    
    IEnumerator SpawnPowerUps()
    {
        while (gameRunning)
        {
            if (spawnPoints.Length > 0)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject powerUp = powerUps[Random.Range(0, powerUps.Length)];
                
                Instantiate(powerUp, spawnPoint.position, Quaternion.identity);
            }
            
            yield return new WaitForSeconds(powerUpSpawnRate);
        }
    }
    
    IEnumerator GameTimer()
    {
        yield return new WaitForSeconds(gameDuration);
        EndGame();
    }
    
    void HandlePlayerMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        player.GetComponent<Rigidbody>().velocity = movement * 10f;
        
        // Complex rotation based on movement
        if (movement != Vector3.zero)
        {
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, 
                Quaternion.LookRotation(movement), 0.1f);
        }
    }
    
    void UpdateGameUI()
    {
        // In a real Unity project, this would update the UI
        Debug.Log("Time: " + Mathf.CeilToInt(timer) + " | Score: " + score);
    }
    
    public void AddScore(int points)
    {
        score += points;
    }
    
    public void EndGame()
    {
        gameRunning = false;
        Debug.Log("Game Over! Final Score: " + score);
        // In a real game, this would show a game over screen
    }
}