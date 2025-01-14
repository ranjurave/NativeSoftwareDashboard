﻿using System.Diagnostics.CodeAnalysis;

namespace ConsoleUser.Models.Domain
{
    public class DashboardGeneralInfo
    {
        [AllowNull] public int id { get; set; }
        [AllowNull] public int ActiveTickets { get; set; }
        [AllowNull] public int UrgentTickets { get; set; }
        [AllowNull] public int ClosedTickets { get; set; }
    }
}

