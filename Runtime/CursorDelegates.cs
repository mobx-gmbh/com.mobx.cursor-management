using UnityEngine;

namespace MobX.CursorManagement
{
    public delegate void LockStateDelegate(CursorLockMode lockState);

    public delegate void CursorFileDelegate(CursorFile cursorFile);

    public delegate void VisibilityDelegate(bool visible);
}