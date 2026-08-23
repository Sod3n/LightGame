namespace LightGame.Features.PushableObject
{
    public class ManualInputManager : PlayerInputManager
    {
        public ManualInputManager(PlayerMain player, PlayerData playerData) : base(player, playerData)
        {
        }

        protected override void OnEnable()
        {
        }
    }
}