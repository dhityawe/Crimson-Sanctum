using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GabrielBigardi.SpriteAnimator
{
    public class UISpriteAnimator : MonoBehaviour
    {
        [SerializeField] private bool _playAutomatically = false;
        [SerializeField] private SpriteAnimationObject _spriteAnimationObject;
        [SerializeField] private Image _imageRenderer;
        
        private SpriteAnimation _currentAnimation;
        private float _animationTime = 0f;
        private bool _paused = false;
        private bool _animationCompleted = false;
        
        public event Action OnAnimationComplete;

        private void OnEnable()
        {
            if (_playAutomatically && _spriteAnimationObject.SpriteAnimations.Count > 0)
            {
                Play(_spriteAnimationObject.SpriteAnimations[0]);
            }
        }

        private void LateUpdate()
        {
            if (_paused)
            {
                Debug.Log("Animation Paused");
                return;
            }

            if (_imageRenderer == null)
            {
                Debug.LogError("Image Renderer is NULL!");
                return;
            }

            Sprite currentFrame = UpdateAnimation(Time.deltaTime);
            if (currentFrame != null)
            {
                _imageRenderer.sprite = currentFrame;
            }
        }


        public UISpriteAnimator Play(string name, int startFrame = 0)
        {
            SpriteAnimation animation = GetAnimationByName(name);
            if (animation != null)
            {
                Play(animation, startFrame);
            }
            return this;
        }

        public UISpriteAnimator Play(SpriteAnimation spriteAnimation, int startFrame = 0)
        {
            if (spriteAnimation == null || spriteAnimation.Frames.Count == 0)
            {
                Debug.LogError("Invalid or empty animation");
                return null;
            }

            _paused = false;
            ChangeAnimation(spriteAnimation);
            SetCurrentFrame(startFrame);

            return this;
        }



        public void Pause() => _paused = true;
        public void Resume() => _paused = false;

        private void ChangeAnimation(SpriteAnimation spriteAnimation)
        {
            _animationCompleted = false;
            _animationTime = 0f;
            OnAnimationComplete = null;
            _currentAnimation = spriteAnimation;
        }

        private Sprite UpdateAnimation(float deltaTime)
        {
            if (_currentAnimation == null)
                return null;

            if (!_animationCompleted)
            {
                _animationTime += deltaTime * _currentAnimation.FPS;
                int totalFrames = _currentAnimation.Frames.Count;

                if (_animationTime >= totalFrames)
                {
                    _animationTime = _currentAnimation.SpriteAnimationType == SpriteAnimationType.Looping ? 0f : totalFrames - 1;
                    _animationCompleted = _currentAnimation.SpriteAnimationType != SpriteAnimationType.Looping;
                    OnAnimationComplete?.Invoke();
                }
            }

            return GetAnimationFrame();
        }


        private Sprite GetAnimationFrame()
        {
            int frameIndex = Mathf.Min((int)_animationTime, _currentAnimation.Frames.Count - 1);
            return _currentAnimation.Frames[frameIndex];
        }

        private SpriteAnimation GetAnimationByName(string name)
        {
            var animation = _spriteAnimationObject.SpriteAnimations.Find(a => a.Name == name);
            return animation;
        }


        public void ChangeAnimationObject(SpriteAnimationObject spriteAnimationObject)
        {
            _spriteAnimationObject = spriteAnimationObject;
        }

        public void SetCurrentFrame(int frame)
        {
            if (_currentAnimation == null || frame < 0 || frame >= _currentAnimation.Frames.Count)
            {
                Debug.LogError($"Invalid frame index {frame} for animation '{_currentAnimation?.Name}' with {_currentAnimation?.Frames.Count} frames");
                return;
            }

            _animationTime = frame;
            _imageRenderer.sprite = _currentAnimation.Frames[frame]; // Update UI sprite immediately
        }

    }
}
