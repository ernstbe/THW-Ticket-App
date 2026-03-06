namespace THWTicketApp.Messages
{
    public sealed class TicketUpdatedMessage
    {
        public string TicketId { get; }
        public TicketUpdatedMessage(string ticketId) => TicketId = ticketId;
    }
}
