using System;
using Assets.Scripts.Player;
using UnityEngine;

public class HealthUI : MonoBehaviour
{
    private PlayerHealth _playerHealth;
    [SerializeField] private GameObject _healthPrefab;
    
    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int currentHealth)
    {
        // Loop through all health UI objects
        for (int i = 0; i < transform.childCount; i++)
        {
            // Activate health UI if index is less than current health
            // Deactivate if index is greater than or equal to current health
            if (i < currentHealth)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (_playerHealth != null)
        {
            InstantiateHealthUI(_playerHealth.MaxHealth);
            // Manually call to display initial health
            HandleHealthChanged(_playerHealth.CurrentHealth);
            Debug.Log("test");
        }
    }

    private void InstantiateHealthUI(int totalHealth)
    {
        for (int i = 0; i < totalHealth; i++)
        {
            Instantiate(_healthPrefab, gameObject.transform);
        }
    }
}
