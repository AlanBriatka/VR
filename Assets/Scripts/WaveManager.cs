using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public int enemyCount;
        public float spawnRate;
        public GameObject enemyPrefab;
    }

    [Header("Wave Configuration")]
    [SerializeField] private List<Wave> waves;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenWaves = 10f;

    [Header("Difficulty Scaling")]
    [SerializeField] private float speedIncreasePerWave = 0.1f;
    [SerializeField] private float healthIncreasePerWave = 10f;

    private int currentWaveIndex = 0;
    private int activeEnemies = 0;
    private bool isSpawning = false;

    private static WaveManager instance;
    public static WaveManager Instance => instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartWaveSequence());
    }

    private IEnumerator StartWaveSequence()
    {
        while (currentWaveIndex < waves.Count)
        {
            Debug.Log($"Starting Wave: {waves[currentWaveIndex].waveName}");
            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));

            while (activeEnemies > 0)
            {
                yield return new WaitForSeconds(1f);
            }

            // Last Kill Juice
            if (TimeManipulation.Instance != null)
            {
                TimeManipulation.Instance.EnableSlowMotion();
                StartCoroutine(StopSlowMoAfterDelay(2f));
            }

            Debug.Log("Wave Clear! Waiting for next wave...");
            currentWaveIndex++;
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("All Waves Complete!");
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;
        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnEnemy(wave.enemyPrefab);
            yield return new WaitForSeconds(wave.spawnRate);
        }
        isSpawning = false;
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        BasicEnemy enemy = enemyObj.GetComponent<BasicEnemy>();
        if (enemy != null)
        {
            float healthScale = 1f + (currentWaveIndex * healthIncreasePerWave / 100f);
            float speedScale = 1f + (currentWaveIndex * speedIncreasePerWave);
            enemy.SetDifficulty(healthScale, speedScale);
            activeEnemies++;
        }
    }

    public void EnemyDied()
    {
        activeEnemies--;
    }

    private IEnumerator StopSlowMoAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (TimeManipulation.Instance != null)
        {
            TimeManipulation.Instance.DisableSlowMotion();
        }
    }
}
