using System;

namespace Light_and_controller.Scripts.Events
{
    public class PendingHealEvent
    {
        public int Amount;
        public float Duration;
        public Action Cancel;
    }
}