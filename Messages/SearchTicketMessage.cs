using CommunityToolkit.Mvvm.Messaging.Messages;

namespace THWTicketApp.Messages;

public class SearchTicketMessage : ValueChangedMessage<string>
{
    public SearchTicketMessage(string searchText) : base(searchText) { }
}
