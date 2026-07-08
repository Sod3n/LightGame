namespace LightGame.Core
{
    public interface IWeight
    {
        public float Get();
    }

    public class WeightRequestEvent
    {
        public float Weight { get; set; }
    }
}