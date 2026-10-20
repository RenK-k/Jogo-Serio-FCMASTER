using System.Collections.Generic;

namespace CoachMaster.Career
{
    public sealed class CareerNotification
    {
        public string Heading;
        public string Message;
    }

    public static class CareerNotificationQueue
    {
        const int MaxPending = 8;
        static readonly Queue<CareerNotification> Pending = new Queue<CareerNotification>();

        public static void Enqueue(string heading, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            if (Pending.Count >= MaxPending)
                Pending.Dequeue();
            Pending.Enqueue(new CareerNotification
            {
                Heading = string.IsNullOrWhiteSpace(heading) ? "CONSEQUÊNCIA" : heading.Trim(),
                Message = message.Trim()
            });
        }

        public static bool TryDequeue(out CareerNotification notification)
        {
            if (Pending.Count == 0)
            {
                notification = null;
                return false;
            }

            notification = Pending.Dequeue();
            return true;
        }

        public static void Clear()
        {
            Pending.Clear();
        }
    }
}
