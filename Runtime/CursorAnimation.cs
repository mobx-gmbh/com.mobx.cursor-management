using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.CursorManagement
{
    /// <summary>
    ///     Class for storing custom cursor animation data
    /// </summary>
    [CreateAssetMenu(menuName = "Cursor/Cursor-Animation", fileName = "Cursor-Animation", order = 100)]
    public class CursorAnimation : CursorFile
    {
        [Title("Animation")]
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] public CursorAnimationType cursorAnimationType;
        [SerializeField] public Texture2D[] frames;

        internal WaitForSeconds Delay { get; private set; }

        public static implicit operator Texture2D(CursorAnimation file)
        {
            return file ? file.frames?.Length > 0 ? file.frames[0] : null : null;
        }

        private void OnEnable()
        {
            Delay = new WaitForSeconds(1 / framesPerSecond);
        }

        private void OnValidate()
        {
            Delay = new WaitForSeconds(1 / framesPerSecond);
        }

#if UNITY_EDITOR

        [Button]
        [HorizontalGroup("Debug")]
        private void SetActiveCursor()
        {
            if (Application.isPlaying)
            {
                CursorManager.Singleton.AddCursorOverride(this);
            }
        }

        [Button]
        [HorizontalGroup("Debug")]
        private void RemoveActiveCursor()
        {
            if (Application.isPlaying)
            {
                CursorManager.Singleton.RemoveCursorOverride(this);
            }
        }
#endif
    }
}