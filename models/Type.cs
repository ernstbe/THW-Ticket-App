namespace THWTicketApp.Models;

public class Type
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Priority> Priorities { get; set; }
    }
