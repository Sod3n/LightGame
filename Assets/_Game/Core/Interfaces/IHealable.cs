namespace Light_and_controller.Scripts.Components
{
    public interface IHealable
    {
        public void Heal(int amount);
    }

    public class HealEvent
    {
        public int Amount { get; set; }
    }
}