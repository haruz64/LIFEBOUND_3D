﻿using Assets.Scripts.Managers;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Core
{
    public class HealthSystem : NetworkBehaviour
    {
        [Header("Health System")]
        private int currentHealth;
        private int maxHealth = 100;
        private bool isPlayerDead = false;
        public bool IsPlayerDead => isPlayerDead;

        [Header("Regeneration System")]
        private float regenerationRate = 0.1f;
        private float healTimer;

        [Header("Components")]
        private Animator animator;
        private GameManager gameManager;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private CameraShake cameraShake;

        [Header("Audio")]
        [SerializeField] private AudioClip hurtSound;

        [Header("Death System")]
        [SerializeField] private GameObject deathCanvas;
        [SerializeField] private TMP_Text deathText;
        private float disconnectTimer;
        private int timeBeforeDisconnect = 5;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            gameManager = GameManager.Instance;
            if (!IsOwner) return;
            deathCanvas.SetActive(false);
            currentHealth = maxHealth;
            healthText.SetText($"Health: {currentHealth}");
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        #region Health System
        public void ApplyHealOverTime()
        {
            healTimer += Time.deltaTime;
            if (healTimer >= regenerationRate)
            {
                Heal(1);
                healTimer = 0f;
            }
        }

        public void Heal(int amount)
        {
            if (!IsLocalPlayer) return;
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            UpdateHealth(currentHealth);
        }

        public void UpdateHealth(int health)
        {
            currentHealth = health;
            healthText.SetText($"Health: {health}");
            Mathf.Clamp(currentHealth, 0, maxHealth);
        }
        #endregion

        #region Damage System

        public void TakeDamage(int damage)
        {
            if (!IsLocalPlayer) return;
            if (isPlayerDead) return;

            if (currentHealth <= 0)
            {
                isPlayerDead = true;
                animator.SetTrigger("IsDead");
                KillPlayer();
            }

            currentHealth -= damage;
            cameraShake.ShakeCamera();
            AudioManager.Instance.PlaySound(hurtSound);

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            UpdateHealth(currentHealth);
        }
        #endregion

        #region Death System
        public void SetPlayerHealth(int health)
        {
            currentHealth = health;
            healthText.SetText($"Health: {health}");
        }
        #endregion

        #region Kill System
        public void KillPlayer()
        {
            isPlayerDead = true;
            deathCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            disconnectTimer = timeBeforeDisconnect;
            StartCoroutine(ShowGameOverMessages());
        }

        private IEnumerator ShowGameOverMessages()
        {
            while (disconnectTimer > 0)
            {
                yield return new WaitForSeconds(1f);
                disconnectTimer--;
            }
            GameManager.Instance.DisconnectAllPlayers();
        }
        #endregion

        public void ForceKillPlayer()
        {
            isPlayerDead = true;
            animator.SetTrigger("IsDead");
            KillPlayer();
        }
    }
}
