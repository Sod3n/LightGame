using System;

namespace Light_and_controller.Scripts.Events
{
    public class PendingDamageEvent
    {
        public int Amount;
        public float Duration;
        public Action Cancel;
    }
}