namespace Light_and_controller.Scripts.Components
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