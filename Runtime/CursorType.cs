using MobX.Mediator.Enums;
using MobX.Mediator.Registry;
using MobX.Utilities.Reflection;
using Sirenix.OdinInspector;

namespace MobX.CursorManagement
{
    [AddressablesGroup("Cursor")]
    public class CursorType : EnumAsset<CursorType>
    {
        [ReadOnly]
        public int SingletonInstanceId { get; set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            SingletonInstanceId = AssetRegistry.Singleton.GetInstanceID();
        }
    }
}