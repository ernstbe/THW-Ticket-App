using System;

namespace THWTicketApp.Utils
{
    public static class NotificationCenter
    {
        public static event Action<string>? TicketUpdated;

        public static void RaiseTicketUpdated(string ticketId)
        {
            TicketUpdated?.Invoke(ticketId);
        }
    }
}
