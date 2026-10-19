namespace LightGame.Core
{
    public interface IDamageable
    {
        public void TakeDamage(int amount);
    }

    public class DamageEvent
    {
        public int Amount { get; set; }
        // World point where the hit landed, when the source has one (hazards); null for damage over time.
        public UnityEngine.Vector2? Point { get; set; }
    }
}