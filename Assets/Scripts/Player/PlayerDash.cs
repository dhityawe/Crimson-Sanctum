using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core.Managers;
using UnityEngine;
using GabrielBigardi.SpriteAnimator;
using CrimsonSanctum.Audio;

namespace Assets.Scripts.Player 
{
    public class PlayerDash : MonoBehaviour, IPlayerAbility
    {
        [Header("Dash Settings")]
        [SerializeField] protected float _dashDuration = 0.2f;
        [SerializeField] protected float _dashSpeed = 20f;
        [SerializeField] protected float _cooldownTime;

        [Header("Audio Settings")]
        [SerializeField] private List<AudioClip> _sfxList;

        private PlayerMove _playerMove;
        private Rigidbody2D _rb;
        private bool _isDashing;
        private bool _isCooldown;
        private float _dashTimer;
        private Vector2 _dashDirection;
        private float _lastLinearVelocityX;
        private PlayerStateManager _stateManager;
        private PlayerHealth _playerHealth;
        private AfterImageEffect _afterImageEffect;
        private Dictionary<string, AudioSource> _dashAudioSources = new Dictionary<string, AudioSource>();

        [Header("Animator")]
        [SerializeField] private SpriteAnimator _spriteAnimator;

        #region Audio Management
        
        /// <summary>
        /// Creates a persistent AudioSource for dash sounds
        /// </summary>
        private AudioSource CreateDashAudioSource(string name, AudioClip clip, float volume = 1f, bool loop = false)
        {
            if (clip == null) return null;
            
            // Check if AudioSource already exists
            if (_dashAudioSources.ContainsKey(name))
            {
                var existingSource = _dashAudioSources[name];
                if (existingSource != null)
                {
                    existingSource.clip = clip;
                    existingSource.volume = volume;
                    existingSource.loop = loop;
                    return existingSource;
                }
            }
            
            // Create new AudioSource GameObject as child
            GameObject audioObject = new GameObject($"DashAudio_{name}");
            audioObject.transform.SetParent(transform);
            audioObject.transform.localPosition = Vector3.zero;
            
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            
            _dashAudioSources[name] = source;
            return source;
        }
        
        /// <summary>
        /// Stops a specific dash AudioSource
        /// </summary>
        private void StopDashAudioSource(string name)
        {
            if (_dashAudioSources.ContainsKey(name))
            {
                var source = _dashAudioSources[name];
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }
        
        /// <summary>
        /// Cleanup all dash AudioSources
        /// </summary>
        private void CleanupDashAudioSources()
        {
            foreach (var kvp in _dashAudioSources)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Stop();
                    Destroy(kvp.Value.gameObject);
                }
            }
            _dashAudioSources.Clear();
        }
        
        #endregion

        #region Events
        public event Action OnEndDash;
        #endregion

        #region IPlayerAbility Implementation
        public bool IsActive { get; private set; }
        
        public bool CanActivate()
        {
            return _stateManager == null || 
                   (_stateManager.CurrentState != PlayerState.Preview &&
                    _stateManager.CurrentState != PlayerState.Climbing && 
                    _stateManager.CurrentState != PlayerState.Dead &&
                    !_isDashing && !_isCooldown);
        }
        
        public void Activate()
        {
            if (CanActivate())
            {
                StartDash();
            }
        }
        
        public void Deactivate()
        {
            if (_isDashing)
            {
                CancelDash();
            }
        }
        
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
        #endregion

        protected virtual void Start()
        {
            _playerMove = GetComponent<PlayerMove>();
            _rb = _playerMove.GetComponent<Rigidbody2D>();
            _stateManager = GetComponent<PlayerStateManager>();
            _playerHealth = GetComponent<PlayerHealth>();
            _afterImageEffect = GetComponent<AfterImageEffect>();
            IsActive = true;
            
            // Subscribe to health events to cancel dash when hit
            if (_playerHealth != null)
            {
                PlayerHealth.OnInvulnerabilityStart += OnPlayerHit;
            }
        }
        
        protected virtual void OnDestroy()
        {
            // Unsubscribe from health events
            PlayerHealth.OnInvulnerabilityStart -= OnPlayerHit;
            
            // Cleanup persistent AudioSources
            CleanupDashAudioSources();
        }
        
        /// <summary>
        /// Called when player takes damage - cancels dash to allow knockback
        /// </summary>
        private void OnPlayerHit()
        {
            if (_isDashing)
            {
                // Cancel dash WITHOUT resetting velocity - let knockback apply
                // Only play Move animation if not climbing
                if (_stateManager == null || _stateManager.CurrentState != PlayerState.Climbing)
                {
                    _spriteAnimator.Play("Move");
                }
                _isDashing = false;
                
                // Stop dash loop SFX
                StopDashAudioSource("DashLoop");
                
                OnEndDash?.Invoke();
                // Don't call CancelDash() because it resets velocity!
            }
        }

        protected virtual void Update()
        {
            if (!_playerMove.CanMove()) return;

            if (!_isDashing && GameInput.Instance.IsDashPressed() && !_isCooldown)
            {
                StartDash();
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    EndDash();
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_isDashing)
            {
                // Freeze Y velocity for flying effect
                _rb.linearVelocity = new Vector2(_dashDirection.x * _dashSpeed, 0f);
            }
        }

        protected virtual void StartDash()
        {
            _lastLinearVelocityX = _rb.linearVelocityX;
            _isDashing = true;
            _isCooldown = true;
            _spriteAnimator.Play("StartSkill").SetOnComplete(() => _spriteAnimator.Play("OnSkill"));
            
            // Play dash start SFX (index 0)
            if (_sfxList != null && _sfxList.Count > 0 && _sfxList[0] != null)
            {
                AudioSource startSource = CreateDashAudioSource("DashStart", _sfxList[0], 0.5f, false);
                if (startSource != null)
                {
                    startSource.Play();
                }
            }
            
            // Play dash loop SFX (index 1) - looping while dashing
            if (_sfxList != null && _sfxList.Count > 1 && _sfxList[1] != null)
            {
                AudioSource loopSource = CreateDashAudioSource("DashLoop", _sfxList[1], 0.5f, true);
                if (loopSource != null)
                {
                    loopSource.Play();
                }
            }
            
            StartCoroutine(StartCooldownDash(_cooldownTime));
            _dashTimer = _dashDuration;
            _dashDirection = _playerMove.IsFlipX() ? Vector2.left : Vector2.right;
            
            // Start afterimage effect
            if (_afterImageEffect != null)
            {
                _afterImageEffect.StartAfterimage();
            }
        }

        public void CancelDash()
        {
            if (_isDashing)
            {
                // Only play Move animation if not climbing
                if (_stateManager == null || _stateManager.CurrentState != PlayerState.Climbing)
                {
                    _spriteAnimator.Play("Move");
                }
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                
                // Stop dash loop SFX
                StopDashAudioSource("DashLoop");
                
                OnEndDash?.Invoke();
            }
        }

        protected virtual void EndDash()
        {
            // Stop dash loop SFX
            StopDashAudioSource("DashLoop");
            
            if (_stateManager != null && _stateManager.CurrentState == PlayerState.Climbing)
            {
                // Just stop dashing, let climb animation continue
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                OnEndDash?.Invoke();
            }
            else
            {
                // Normal end dash sequence
                _spriteAnimator.Play("EndSkill").SetOnComplete(() => _spriteAnimator.Play("Move"));
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                OnEndDash?.Invoke();
            }
        }

        protected virtual IEnumerator StartCooldownDash(float cooldownTime)
        {
            if (_isCooldown)
            {
                float elapsed = 0f;
                while (elapsed < cooldownTime)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                _isCooldown = false;
            }
        }
    }
}
