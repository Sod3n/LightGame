using System;

namespace LightGame.Events
{
    public class PendingDamageEvent
    {
        public int Amount;
        public float Duration;
        public Action Cancel;
    }
}