﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhyNotEarth.Meredith.Public
{
    public class EmailEvent
    {
        public int Id { get; set; }

        public int EmailId { get; set; }

        public Email Email { get; set; } = null!;

        public EmailEventType Type { get; set; }

        public DateTime DateTime { get; set; }
    }

    public enum EmailEventType : byte
    {
        None = 0,
        Delivered = 1,
        Opened = 2,
        Clicked = 3
    }
}