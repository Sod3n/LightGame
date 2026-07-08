using Light_and_controller.Scripts.AbilitiesSystem;
using UnityEngine.AddressableAssets;

namespace Light_and_controller.Scripts
{
    public static class GD
    {
        public static LevelOrder LevelOrder;
        public static LevelAbilities LevelAbilities;
        public static ObjectsWeight ObjectsWeight;
        private static bool _isInitialized;
        
        public static void Init()
        {
            if (_isInitialized) return;
            
            LevelOrder = Addressables.LoadAssetAsync<LevelOrder>("LevelOrder").WaitForCompletion();
            LevelAbilities = Addressables.LoadAssetAsync<LevelAbilities>("LevelAbilities").WaitForCompletion();
            ObjectsWeight = Addressables.LoadAssetAsync<ObjectsWeight>("ObjectsWeight").WaitForCompletion();
            
            // Load abilities from Addressables if configured
            LevelAbilities?.LoadAbilitiesFromAddressables();
            
            _isInitialized = true;
        }
    }
}