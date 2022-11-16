﻿namespace RaceCorp.Data.Models
{
    using System;

    using RaceCorp.Data.Common.Models;

    public class Message : BaseDeletableModel<string>
    {
        public string ConversatioId { get; set; }

        public virtual Conversation Conversation { get; set; }

        public string SenderId { get; set; }

        public virtual ApplicationUser Sender { get; set; }

        public string RevceiverId { get; set; }

        public virtual ApplicationUser Receiver { get; set; }

        public string Content { get; set; }
    }
}
