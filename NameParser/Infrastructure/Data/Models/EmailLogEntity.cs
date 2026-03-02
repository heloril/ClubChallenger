using System;

namespace NameParser.Infrastructure.Data.Models
{
    public class EmailLogEntity
    {
        public int Id { get; set; }
        public string EmailType { get; set; } // "Challenge" or "Member"
        public int? ChallengeId { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientName { get; set; }
        public string Subject { get; set; }
        public DateTime SentDate { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsTest { get; set; }
        public string SentBy { get; set; } // User who sent the email
    }
}
