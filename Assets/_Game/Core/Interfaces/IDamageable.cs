namespace LightGame.Core
{
    public interface IDamageable
    {
        public void TakeDamage(int amount);
    }

    public class DamageEvent
    {
        public int Amount { get; set; }
    }
}