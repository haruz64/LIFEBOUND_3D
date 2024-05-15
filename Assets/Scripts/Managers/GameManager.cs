using UnityEngine;
using Unity.Netcode;
using Assets.Scripts.Core;

public class GameManager: NetworkBehaviour
{
    [SerializeField] private GameObject networkManagerGameObject;
    public static GameManager Instance { get; private set; }
    [Header("Checkpoint")]
    [SerializeField] private GameObject defaultSpawn;
    public GameObject DefaultSpawn => defaultSpawn;
    private Vector3 lastInteractedCheckpointPosition;
    public Vector3 LastInteractedCheckpointPosition
    {
        get => lastInteractedCheckpointPosition;
        set => lastInteractedCheckpointPosition = value;
    }


    [Header("Soul Swap")]
    private float soulSwapCooldown = 30f;
    public float SoulSwapCooldown => soulSwapCooldown;

    [Header("Heat Wave")]
    private bool isHeatWaveActivated;
    private bool isCountdownActivated;
    private int heatWaveDamage = 1;
    private float heatTickInterval = 10f;
    private float heatWaveTimer = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isHeatWaveActivated = true;
        isCountdownActivated = true;
        networkManagerGameObject = GameObject.FindGameObjectWithTag("NetworkManager");

        if (!IsServer) return;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            if (isHeatWaveActivated)
            {
                heatWaveTimer += Time.deltaTime;
                if (heatWaveTimer >= heatTickInterval)
                {
                    ApplyHeatwaveDamageServerRpc();
                    heatWaveTimer = 0f;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (IsServer)
        {
            if (isCountdownActivated)
            {
                StartCountdownServerRpc();
            }
        }
        CheckForPlayerDeathStateServerRpc();
    }

    #region Countdown
    [ServerRpc(RequireOwnership = false)]
    private void StartCountdownServerRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out CountdownTimer timer))
            {
                timer.UpdateCountdownTimerServerRpc();
            }
        }
    }

    [ClientRpc]
    public void StartCountdownClientRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out CountdownTimer timer))
            {
                timer.UpdateCountdownTimer();
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ApplyCountdownEndServerRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out CountdownTimer timer))
            {
                if (timer.IsTimerRunOut)
                {
                    ApplyCountdownEndClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    public void ApplyCountdownEndClientRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out HealthSystem health))
            {
                health.KillPlayer();
            }
        }
    }
    #endregion

    public void DisconnectAllPlayers(ulong clientId)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if(player.TryGetComponent(out NetworkObject networkObject))
            {
                if (networkObject.OwnerClientId == clientId)
                {
                    NetworkManager.Singleton.DisconnectClient(clientId);
                    DisconnectAllPlayersToMainMenu();
                }
            }
        }
    }

    public void DisconnectAllPlayersToMainMenu()
    {
        Destroy(networkManagerGameObject);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    #region Death System
    [ServerRpc(RequireOwnership = false)]
    private void CheckForPlayerDeathStateServerRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out HealthSystem health))
            {
                if (health.IsPlayerDead)
                {
                    ApplyDeathStateClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    public void ApplyDeathStateClientRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out HealthSystem health))
            {
                health.KillPlayer();
            }
        }
    }
    #endregion

    #region RPCs (Heat Wave)
    [ServerRpc(RequireOwnership = false)]
    private void ApplyHeatwaveDamageServerRpc()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if(player.TryGetComponent(out HealthSystem health))
            {
                ApplyHeatwaveDamageClientRpc(heatWaveDamage);
            }
        }
    }

    [ClientRpc]
    public void ApplyHeatwaveDamageClientRpc(int damage)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.TryGetComponent(out HealthSystem health))
            {
                health.TakeDamage(damage);
            }
        }
    }
    #endregion
}