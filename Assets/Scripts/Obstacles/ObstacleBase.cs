using UnityEngine;
using System.Collections;

public abstract class ObstacleBase : MonoBehaviour
{
    // 🎨 Shared Config — Designer can assign in Inspector
    public ObstacleData data;

    // 🧠 Lifecycle — Called automatically
    protected virtual void Awake()
    {
        Initialize(); // Force all obstacles to init
    }

    // 💥 Collision — Shared player hit logic
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerHit(); // Each obstacle defines what "hit" means
        }
    }

    // 🧭 ABSTRACT METHODS — Each obstacle MUST implement these
    protected abstract void Initialize();
    protected abstract void OnPlayerHit();

    // 🎇 SHARED UTILITIES — Reusable by all obstacles
    protected void PlayHitEffect()
    {
        if (data?.hitSFX != null)
            // SoundManager.Instance?.Play(data.hitSFX);

        StartCoroutine(FlashColor(data?.hitFlashColor ?? Color.red, data?.hitFlashDuration ?? 0.2f));
    }

    protected IEnumerator FlashColor(Color color, float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color original = sr.color;
            sr.color = color;
            yield return new WaitForSeconds(duration);
            sr.color = original;
        }
    }
}