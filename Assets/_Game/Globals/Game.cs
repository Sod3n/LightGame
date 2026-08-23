using LightGame.Features.Abilities;
using UnityEngine.AddressableAssets;

namespace LightGame.Globals
{
    public static class Game
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