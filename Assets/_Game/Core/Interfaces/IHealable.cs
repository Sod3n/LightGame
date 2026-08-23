namespace LightGame.Core
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