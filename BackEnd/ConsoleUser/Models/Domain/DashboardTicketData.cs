﻿using System.Diagnostics.CodeAnalysis;

namespace ConsoleUser.Models.Domain
{
    public class DashboardTicketData
    {
        [AllowNull] public int id { get; set; }
        [AllowNull] public string Organisation { get; set; }
        [AllowNull] public string Subject { get; set; }
        [AllowNull] public string Status { get; set; }
        [AllowNull] public string Recipient { get; set; }
        [AllowNull] public bool Billable { get; set; }
        [AllowNull] public string Priority { get; set; }
        [AllowNull] public string RequestedDate { get; set; } // TODO confirm requested or created time
        [AllowNull] public string TimeDue { get; set; }
        [AllowNull] public string Type { get; set; }
        [AllowNull] public string url { get; set; }
        [AllowNull] public string TrafficLight { get; set; }
    }
}

