using System;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable, IHealable
{
    [SerializeField] private int health;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private int nearDeathThreshold = 1;

    public Action<int> OnTakeDamage;
    public Action<int> OnHeal;
    public Action<bool> OnNearDeathStateChanged;

    private float invincibilityTimer = 0f;
    private bool isInNearDeathState = false;
    
    public int Health
    {
        get => health;
        protected set => health = value;
    }

    public int MaxHealth { get; set; }
    
    public bool IsInvincible => invincibilityTimer > 0f;

    public bool IsInNearDeathState => isInNearDeathState;

    protected virtual void Awake()
    {
        MaxHealth = Health;
    }

    protected virtual void Update()
    {
        if(invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    protected virtual void OnEnable()
    {
        EventBus.Subscribe<DamageEvent>(gameObject, OnDamageEvent);
        EventBus.Subscribe<HealEvent>(gameObject, OnHealEvent);
    }

    protected virtual void OnDisable()
    {
        EventBus.Unsubscribe<DamageEvent>(gameObject, OnDamageEvent);
        EventBus.Unsubscribe<HealEvent>(gameObject, OnHealEvent);
    }

    private void OnDamageEvent(DamageEvent evt)
    {
        TakeDamage(evt.Amount);
    }

    private void OnHealEvent(HealEvent evt)
    {
        Heal(evt.Amount);
    }

    public virtual void TakeDamage(int amount)
    {
        if(IsInvincible) return;
        
        Health -= amount;
        OnTakeDamage?.Invoke(amount);
        
        if(Health <= 0)
        {
            Die();
        }
        else
        {
            invincibilityTimer = invincibilityDuration;
        }

        CheckNearDeathState();
    }

    public virtual void Heal(int amount)
    {
        Health = Mathf.Min(Health + amount, MaxHealth);
        OnHeal?.Invoke(amount);
        CheckNearDeathState();
    }
    
    public void ClampHealthToMax()
    {
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
    }

    private void CheckNearDeathState()
    {
        bool shouldBeInNearDeath = Health > 0 && Health <= nearDeathThreshold;

        if (shouldBeInNearDeath != isInNearDeathState)
        {
            isInNearDeathState = shouldBeInNearDeath;
            OnNearDeathStateChanged?.Invoke(isInNearDeathState);
        }
    }

    public virtual void Die() {}
}
