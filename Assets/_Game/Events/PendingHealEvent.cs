using System;

namespace LightGame.Events
{
    public class PendingHealEvent
    {
        public int Amount;
        public float Duration;
        public Action Cancel;
    }
}