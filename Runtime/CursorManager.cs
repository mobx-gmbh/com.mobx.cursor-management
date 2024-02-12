using MobX.Inspector;
using MobX.Mediator.Callbacks;
using MobX.Mediator.Singleton;
using MobX.Utilities;
using MobX.Utilities.Collections;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MobX.CursorManagement
{
    public class CursorManager : SingletonAsset<CursorManager>
    {
        #region Inspector

        [Foldout("Cursor Assets")]
        [SerializeField] private CursorType startCursor;
        [SerializeField] [Required] private CursorSet startCursorSet;

        [Foldout("Mediator")]
        [SerializeField] [Required] private HideCursorLocks cursorHide;
        [SerializeField] [Required] private ConfineCursorLocks cursorConfine;
        [SerializeField] [Required] private LockCursorLocks cursorLock;

        #endregion


        #region Fields

        [ReadOnly]
        [ShowInInspector]
        private readonly ListStack<CursorFile> _cursorStack = new();

        #endregion


        #region Properties

        public CursorSet ActiveCursorSet { get; private set; }
        public CursorFile ActiveCursor => _cursorStack.Peek();

        public static CursorLockMode LockState
        {
            get => Cursor.lockState;
            private set
            {
                var changed = LockState != value;
                Cursor.lockState = value;
                if (changed)
                {
                    CursorLockModeChanged?.Invoke(value);
                }
            }
        }

        public static bool Visible
        {
            get => Cursor.visible;
            private set
            {
                var changed = Visible != value;
                Cursor.visible = value;
                if (changed)
                {
                    CursorVisibilityChanged?.Invoke(value);
                }
            }
        }

        #endregion


        #region Events

        /// <summary>
        ///     Event is invoked every time the <see cref="UnityEngine.Cursor" /> is updated.<br />
        ///     Event will pass a reference to the new <see cref="CursorFile" />.
        /// </summary>
        public static event CursorFileDelegate CursorChanged;

        /// <summary>
        ///     Event is invoked every time the property <see cref="UnityEngine.Cursor" />.
        ///     <see cref="UnityEngine.Cursor.lockState" /> is updated.<br />
        ///     Event will pass the value of the new <see cref="CursorLockMode" />.
        /// </summary>
        public static event LockStateDelegate CursorLockModeChanged;

        /// <summary>
        ///     Event is invoked every time the property <see cref="UnityEngine.Cursor" />.
        ///     <see cref="UnityEngine.Cursor.visible" /> is updated.<br />
        ///     Event will pass the value of the new <see cref="CursorLockMode" />.
        /// </summary>
        public static event VisibilityDelegate CursorVisibilityChanged;

        #endregion


        #region Setup

        [CallbackOnInitialization]
        protected void Initialize()
        {
            ActiveCursorSet = startCursorSet;
            AddCursorOverride(startCursor);

            cursorHide.FirstAdded += UpdateCursorState;
            cursorHide.LastRemoved += UpdateCursorState;
            cursorConfine.FirstAdded += UpdateCursorState;
            cursorConfine.LastRemoved += UpdateCursorState;
            cursorLock.FirstAdded += UpdateCursorState;
            cursorLock.LastRemoved += UpdateCursorState;
        }

        [CallbackOnApplicationQuit]
        private void Shutdown()
        {
            cursorHide.FirstAdded -= UpdateCursorState;
            cursorHide.LastRemoved -= UpdateCursorState;
            cursorConfine.FirstAdded -= UpdateCursorState;
            cursorConfine.LastRemoved -= UpdateCursorState;
            cursorLock.FirstAdded -= UpdateCursorState;
            cursorLock.LastRemoved -= UpdateCursorState;
            _cursorStack.Clear();
        }

        #endregion


        #region Cursor State

        private void UpdateCursorState()
        {
            Visible = cursorHide.HasAny() is false;

            if (cursorLock.HasAny())
            {
                LockState = CursorLockMode.Locked;
                return;
            }

            if (cursorConfine.HasAny())
            {
                LockState = CursorLockMode.Confined;
                return;
            }

            LockState = CursorLockMode.None;
        }

        #endregion


        #region Cursor Set

        public void SwitchActiveCursorSet(CursorSet cursorSet)
        {
            for (var i = 0; i < _cursorStack.Count; i++)
            {
                var cursorFile = _cursorStack[i];
                var cursorType = ActiveCursorSet.GetType(cursorFile);
                _cursorStack[i] = cursorSet.GetCursor(cursorType);
            }

            ActiveCursorSet = cursorSet;
            UpdateCursorFileInternal();
        }

        #endregion


        #region Cursor Style

        public void AddCursorOverride(CursorType cursorType)
        {
            var cursorById = ActiveCursorSet.GetCursor(cursorType);
            AddCursorOverride(cursorById);
        }

        public void RemoveCursorOverride(CursorType cursorType)
        {
            var cursorById = ActiveCursorSet.GetCursor(cursorType);
            RemoveCursorOverride(cursorById);
        }

        internal void AddCursorOverride(CursorFile file)
        {
            if (file == null)
            {
                return;
            }
            if (file == ActiveCursor)
            {
                return;
            }

            _cursorStack.PushUnique(file);
            UpdateCursorFileInternal();
        }

        internal void RemoveCursorOverride(CursorFile file)
        {
            var isActiveCursor = file == ActiveCursor;
            _cursorStack.Remove(file);
            if (isActiveCursor)
            {
                UpdateCursorFileInternal();
            }
        }

        private void UpdateCursorFileInternal()
        {
            switch (ActiveCursor)
            {
                case CursorTexture cursorTexture:
                    SetCursorTextureInternal(cursorTexture);
                    break;

                case CursorAnimation cursorAnimation:
                    SetCursorAnimationInternal(cursorAnimation);
                    break;

                default:
                    SetNullCursorInternal();
                    break;
            }
        }

        #endregion


        #region Cursor Style Internal

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCursorTextureInternal(CursorTexture file)
        {
            StopCursorAnimation();
            Cursor.SetCursor(file.Texture, file.HotSpot, file.CursorMode);
            CursorChanged?.Invoke(file);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCursorAnimationInternal(CursorAnimation file)
        {
            RunCursorAnimation(file);
            CursorChanged?.Invoke(file);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetNullCursorInternal()
        {
            StopCursorAnimation();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            CursorChanged?.Invoke(null);
        }

        #endregion


        #region Cursor Animation

        private Coroutine _cursorAnimation;
        private bool _hasFocus = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopCursorAnimation()
        {
            if (_cursorAnimation.IsNotNull())
            {
                Gameloop.StopCoroutine(_cursorAnimation);
            }
        }

        private void RunCursorAnimation(CursorAnimation animationFile)
        {
            StopCursorAnimation();

            switch (animationFile.cursorAnimationType)
            {
                case CursorAnimationType.PlayOnce:
                    _cursorAnimation = Gameloop.StartCoroutine(PlayOnce(animationFile));
                    break;
                case CursorAnimationType.Loop:
                    _cursorAnimation = Gameloop.StartCoroutine(Loop(animationFile));
                    break;
                case CursorAnimationType.PingPong:
                    _cursorAnimation = Gameloop.StartCoroutine(PingPong(animationFile));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion


        #region Cursor Animations Coroutines

        private IEnumerator PlayOnce(CursorAnimation file)
        {
            var frames = file.frames;
            var timer = file.Delay;
            var hotSpot = file.HotSpot;
            var cursorMode = file.CursorMode;

            for (var i = 0; i < frames.Length; i++)
            {
                Cursor.SetCursor(frames[i], hotSpot, cursorMode);
                yield return timer;
            }

            _cursorAnimation = null;
        }

        private IEnumerator Loop(CursorAnimation file)
        {
            var frames = file.frames;
            var timer = file.Delay;
            var hotSpot = file.HotSpot;
            var cursorMode = file.CursorMode;
            var frameCount = frames.Length;
            var currentFrame = 0;

            while (_hasFocus)
            {
                Cursor.SetCursor(frames[currentFrame++], hotSpot, cursorMode);
                currentFrame = currentFrame == frameCount ? 0 : currentFrame;
                yield return timer;
            }
        }

        private IEnumerator PingPong(CursorAnimation file)
        {
            var frames = file.frames;
            var timer = file.Delay;
            var hotSpot = file.HotSpot;
            var cursorMode = file.CursorMode;
            var frameCount = frames.Length - 1;
            var increment = 1;
            var frame = 0;

            while (_hasFocus)
            {
                if (frame == frameCount)
                {
                    increment = -1;
                }
                else if (frame == 0)
                {
                    increment = 1;
                }

                Cursor.SetCursor(frames[frame += increment], hotSpot, cursorMode);
                yield return timer;
            }
        }

        #endregion


        #region Application Focus

        [CallbackMethod(Segment.ApplicationFocus)]
        private void OnApplicationFocus(bool focus)
        {
            _hasFocus = focus;
            if (_hasFocus)
            {
                AddCursorOverride(ActiveCursor);
            }
        }

        #endregion
    }
}