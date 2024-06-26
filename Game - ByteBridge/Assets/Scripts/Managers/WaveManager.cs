using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;
    public EnemyManager EnemyManager;
    public LoadoutRNGManager LoadoutRngManager;
    public ApiManager ApiManager;
    private GunManager gm;
    public int waveNumber=1;
    [SerializeField] private GameObject LoadoutUI;
    [SerializeField] private GameObject PlayerUI;
    [SerializeField] private GameObject GameOverUI;
    [SerializeField] private TextMeshProUGUI timeLeftTMP;
    [SerializeField] private TextMeshProUGUI waveCounterTMP;
    private PlayerManager _playerManager;
    public float difficultyModifier = 1f;
    public float difficultyModifierIncrement = 0.3f;
    public float spawnTimeDecrement = 0.02f;
    public int waveDuration = 60;
    public int timeLeft;
    private Coroutine timer;
    public void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        EnemyManager = EnemyManager.Instance;
        LoadoutRngManager = LoadoutRNGManager.Instance;
        ApiManager = ApiManager.Instance;
        EnemyManager.spawning = false;
        _playerManager = PlayerManager.Instance;
        gm = GunManager.Instance;
        GameOverUI.SetActive(false);
        
    }

   

    private void Update()
    {
        timeLeftTMP.text = String.Format("{0}s",timeLeft);
    }


    public void StartNextWave()
    {
        waveNumber++;
        LoadoutUI.SetActive(false);
        PlayerUI.SetActive(true);
        GameOverUI.SetActive(false);
        EnemyManager.spawning = true;
        difficultyModifier += difficultyModifierIncrement;
        EnemyManager.timeBetweenSpawns -= spawnTimeDecrement;
        gm.SpawnGuns();
        waveCounterTMP.text = String.Format("{0}th wave",waveNumber);
        ApplyBeforeWave();
        timeLeft = waveDuration;
        timer = StartCoroutine(Countdown());
        _playerManager.bestWave++;
    }

    public void EndWave()
    {
        
        EnemyManager.spawning = false;
        EnemyManager.DestroyAllEnemies();
        LoadoutUI.SetActive(true);
        PlayerUI.SetActive(false);
        LoadoutRngManager.GetRandomCards();
        gm.RemoveAllGuns();
    }
    private IEnumerator Countdown()
    {
        while (timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft--;
        }

        if (timeLeft == 0)
        {
            EndWave();
        }

    }

    public void GameOver()
    {
        EnemyManager.spawning = false;
        EnemyManager.DestroyAllEnemies();
        PlayerUI.SetActive(false);
        GameOverUI.SetActive(true);
        StartCoroutine(
            LoadoutRngManager.StaggerRandom()
        );
        gm.RemoveAllGuns();
        // first authenticates, then fetches profile and finally updates leaderboard
        StartCoroutine(ApiManager.AuthenticateMockAPI(isAuthenticated =>
        {
            if (isAuthenticated)
            {
                StartCoroutine(ApiManager.FetchProfile(userProfile =>
                {
                    string nic = userProfile.nic;
                    Debug.Log("kills and then waves");
                    Debug.Log(_playerManager.kills);
                    Debug.Log(_playerManager.bestWave);
                    StartCoroutine(ApiManager.UpdateLeaderboard(new ApiManager.LeaderboardEntry(nic,_playerManager.kills,_playerManager.bestWave), updateSuccessful =>
                    {}));
                }));
            }
        }));
    }

    public void RestartGame()
    {
        GameOverUI.SetActive(false);
        PlayerUI.SetActive(false);
        LoadoutUI.SetActive(true);
        StopCoroutine(timer);
        difficultyModifier = 1f;
        EnemyManager.timeBetweenSpawns = 0.75f;
        _playerManager.ResetStats();
        LoadoutRngManager.RestartGame();
        LoadoutRngManager.UpdateInventory();
        


    }

    public void ApplyBeforeWave()
    {
        
        int newHealth =  (int)(_playerManager.maxHealth * _playerManager.regenModifier.StatValue);
        _playerManager.currentHealth = Mathf.Min(_playerManager.currentHealth + newHealth, _playerManager.maxHealth);
    }    
    
    
}
