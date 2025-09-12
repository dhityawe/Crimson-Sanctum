using UnityEngine;
using DG.Tweening;

public class TrapdoorCoffinObstacle : ObstacleBase, IActivatable
{
    [Header("Coffin Behavior")]
    public CoffinTriggerType triggerType = CoffinTriggerType.OnPlayerProximity;
    public float triggerRadius = 2f; // If proximity-based
    public float closeDelay = 1.5f; // Time before auto-close
    public Ease openEase = Ease.InOutQuart;
    public Ease closeEase = Ease.OutBack;

    [Header("Components")]
    public GameObject bodyCollider; // Drag child GameObject here
    public SpriteRenderer coffinLid; // The lid sprite (or whole model)

    [Header("Audio")]
    public string creakSFX = "CoffinCreak";
    public string openSFX = "CoffinOpen";
    public string closeSFX = "CoffinSlam";

    private Collider2D bodyColliderComponent;
    private int originalSortingOrder;
    private bool isOpen = false;

    protected override void Initialize()
    {
        if (bodyCollider == null)
        {
            Debug.LogError("TrapdoorCoffin: bodyCollider not assigned!");
            return;
        }

        bodyColliderComponent = bodyCollider.GetComponent<Collider2D>();
        if (bodyColliderComponent == null)
        {
            Debug.LogError("TrapdoorCoffin: bodyCollider has no Collider2D!");
            return;
        }

        if (coffinLid == null)
        {
            coffinLid = GetComponent<SpriteRenderer>();
            if (coffinLid == null)
            {
                Debug.LogError("TrapdoorCoffin: coffinLid SpriteRenderer not found!");
                return;
            }
        }

        originalSortingOrder = coffinLid.sortingOrder;

        if (triggerType == CoffinTriggerType.OnPlayerProximity)
        {
            enabled = true; // Keep Update active for proximity check
        }
    }

    void Update()
    {
        if (triggerType == CoffinTriggerType.OnPlayerProximity && !isOpen)
        {
            CheckPlayerProximity();
        }
    }

    void CheckPlayerProximity()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < triggerRadius)
            {
                Activate();
            }
        }
    }

    public void Activate()
    {
        if (isOpen) return;

        isOpen = true;

        // ▶️ Play creak SFX

        // ▶️ Animate lid open
        // if animation on complete play OnOpenComplete
        OnOpenComplete(); //? Temporarily call directly
    }

    void OnOpenComplete()
    {
        // ▶️ Disable bodyCollider
        if (bodyColliderComponent != null)
            bodyColliderComponent.enabled = false;

        // ▶️ Set sorting order to 6 (open state)
        coffinLid.sortingOrder = 6;

        // ▶️ Play open SFX
        //! SoundManager.Instance?.Play(openSFX);

        // ▶️ Auto-close after delay
        DOVirtual.DelayedCall(closeDelay, CloseCoffin);
    }

    void CloseCoffin()
    {
        if (!isOpen) return;

        // ▶️ Play close SFX
        //! SoundManager.Instance?.Play(closeSFX);

        // ▶️ Animate lid close
        // if animation on complete play OnCloseComplete
        OnCloseComplete(); //? Temporarily call directly
    }

    void OnCloseComplete()
    {
        // ▶️ Re-enable bodyCollider
        if (bodyColliderComponent != null)
            bodyColliderComponent.enabled = true;

        // ▶️ Restore original sorting order
        coffinLid.sortingOrder = originalSortingOrder;

        isOpen = false;
    }

    public void Deactivate()
    {
        if (isOpen)
            CloseCoffin();
    }

    protected override void OnPlayerHit()
    {
        // Only kill if coffin is OPEN (falling into void)
        if (isOpen)
        {
            PlayHitEffect();
            //! CameraShaker.Instance?.Shake(0.3f, 0.2f);
            //! GameManager.Instance.PlayerDie();
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}

public enum CoffinTriggerType
{
    OnPlayerProximity,
    OnPressurePlate // You can hook this up manually via other script
}