namespace TicketApp.Models;

using System.Collections.Generic;


 public class Ticket
    {
        public string Id { get; set; }
        public Group Group { get; set; }
        public bool Deleted { get; set; }
        public Type Type { get; set; }
        public Priority Priority { get; set; }
        public List<string> Tags { get; set; }
        public string Subject { get; set; }
        public string Issue { get; set; }
        public List<string> Subscribers { get; set; }
        public string Date { get; set; }
        public List<Comment> Comments { get; set; }
        public List<Note> Notes { get; set; }
        public List<Attachment> Attachments { get; set; }
        public List<History> History { get; set; }
        public Status Status { get; set; }
        public User Owner { get; set; }
        public int Uid { get; set; }
        public int __v { get; set; }
        public User Assignee { get; set; }
        public string ClosedDate { get; set; }
    }