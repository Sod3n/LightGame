namespace Light_and_controller.Scripts.Components
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