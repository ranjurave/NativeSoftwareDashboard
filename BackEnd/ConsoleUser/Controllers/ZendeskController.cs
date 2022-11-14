﻿using ConsoleUser.Controllers;
using ConsoleUser.Models.Domain;
using ConsoleUser.Models.DTO;
using ConsoleUser.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace Zendesk.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZendeskController : ControllerBase
    {
        private readonly ICustomerRepository customerRepository;
        private readonly ISupportLevelRepository customerSupportLevelRepository;

        public ZendeskController(ICustomerRepository customerRepository, ISupportLevelRepository customerSupportLevelRepository)
        {
            this.customerRepository = customerRepository;
            this.customerSupportLevelRepository = customerSupportLevelRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var options = new RestClientOptions("https://native9107.zendesk.com")
            {
                ThrowOnAnyError = true,
                MaxTimeout = -1  // 1 second
            };

            var client = new RestClient(options);
            var ticketRequest = new RestRequest("/api/v2/tickets", Method.Get);
            var metricsRequest = new RestRequest("/api/v2/ticket_metrics.json", Method.Get);
            var usersRequest = new RestRequest("/api/v2/users", Method.Get);

            ticketRequest.AddHeader("Authorization", "Basic cmF2ZWVuZHJhbnJhbmp1QGdtYWlsLmNvbTpXZWJkZXZfMjAyMg==");
            //metricsRequest.AddHeader("Authorization", "Basic cmF2ZWVuZHJhbnJhbmp1QGdtYWlsLmNvbTpXZWJkZXZfMjAyMg==");
            usersRequest.AddHeader("Authorization", "Basic cmF2ZWVuZHJhbnJhbmp1QGdtYWlsLmNvbTpXZWJkZXZfMjAyMg==");

            //TODO do we actually need cookies
            ticketRequest.AddHeader("Cookie", "__cfruid=48f83724a725243fd95c678dd50f3dd2d953d978-1667859886; _zendesk_cookie=BAhJIhl7ImRldmljZV90b2tlbnMiOnt9fQY6BkVU--459ed01949a36415c1716b5711271c3d08918307");
            //metricsRequest.AddHeader("Cookie", "__cf_bm=KxlySeaioTuVYvuFk4E5tRI8BOWLks6ByFoscguzUOI-1667859951-0-Ac9xh1i7NuOLkLmngWxRlUgF6yk+oGtLlnHg4MgzJ4VxGYAuk2O39dvMOHMT6RbdxhM5Hqft8CvRxOFHUu7Hqxs6qh6HI1xHSqrrODKXfd/E; __cfruid=d6d7639fe6c226ee32f5b8fed32a369d01cc9511-1667859951; __cfruid=92c90c6eb9ec4f99a11232886662497fe9237eb9-1667859962; _zendesk_cookie=BAhJIhl7ImRldmljZV90b2tlbnMiOnt9fQY6BkVU--459ed01949a36415c1716b5711271c3d08918307");
            usersRequest.AddHeader("Cookie", "__cf_bm=Bvy1dB189VHVzvKE06rhF45I9xeeULscC4tLWYi.uFU-1667868863-0-AcBBs4QbNfD3hICdAoad6zeSnfs8GpkPaqMlf2Uf8+LI1xraXEPOT7uSYsBDF50EB0TOCt4zn4qzdVSpAFImPUcNS7plBTGERKxB9rEa6Isq; __cfruid=d6d7639fe6c226ee32f5b8fed32a369d01cc9511-1667859951; __cfruid=64d825a7586ffffb2903a6ea42c4775ebdaa1f20-1667868990; _zendesk_cookie=BAhJIhl7ImRldmljZV90b2tlbnMiOnt9fQY6BkVU--459ed01949a36415c1716b5711271c3d08918307");

            RestResponse ticketResponse = await client.ExecuteAsync(ticketRequest);
            //RestResponse metricsResponse = await client.ExecuteAsync(metricsRequest);
            RestResponse usersResponse = await client.ExecuteAsync(usersRequest);

            var zendeskData = JsonConvert.DeserializeObject<ZendeskData>(ticketResponse.Content);
            //var zendeskMetrics = JsonConvert.DeserializeObject<ZendeskMetrics>(metricsResponse.Content);
            var zendeskUsers = JsonConvert.DeserializeObject<ZendeskUsers>(usersResponse.Content);

            var customers = customerRepository.GetAll();
            var supportLevel = customerSupportLevelRepository.GetAll();

            // Creating Json for dashboard
            var dashboardTickets = CreateTicketInfo(zendeskData, zendeskUsers, customers, supportLevel);

            return Ok(dashboardTickets);
        }

        // Dashboard specific JSON creation
        private static List<DashboardTicketData> CreateTicketInfo(ZendeskData? zendeskData, ZendeskUsers? zendeskUsers, IEnumerable<ConsoleUser.Models.Customer> customers, IEnumerable<ConsoleUser.Models.CustomerSupportLevel> supportLevel)
        {
            var ticketList = new List<DashboardTicketData>();
            foreach (var ticket in zendeskData.tickets)
            {
                if(ticket.status != "solved" && ticket.status != "closed")
                {
                    var dashboardTicket = new DashboardTicketData();

                    dashboardTicket.id = ticket.id;
                    dashboardTicket.Organisation = GetZendeskUserName(zendeskUsers, ticket);
                    if (ticket.subject != null) dashboardTicket.Subject = ticket.subject;
                    if (ticket.status != null) dashboardTicket.Status = ticket.status;
                    if (ticket.assignee_id != null) dashboardTicket.Recipient = ticket.assignee_id.ToString();//TODO assignee or recepient
                    dashboardTicket.Billable = GetBillableCustomField(ticket.custom_fields[0]);
                    dashboardTicket.Priority = ticket.priority;
                    dashboardTicket.RequestedDate = ticket.created_at.ToLocalTime().ToString();
                    //dashboardTicket.TimeDue = GetTimeDue(dashboardTicket.Organisation, dashboardTicket.Priority, DateTime.Parse(dashboardTicket.RequestedDate), customers, supportLevel).ToString();
                    dashboardTicket.TimeDue = GetTimeDueMinusOffHours(dashboardTicket.Organisation, dashboardTicket.Priority, DateTime.Parse(dashboardTicket.RequestedDate), customers, supportLevel).ToString();
                    if (ticket.type != null) dashboardTicket.Type = ticket.type;
                    if (ticket.url != null) dashboardTicket.url = "https://native9107.zendesk.com/agent/tickets/" + ticket.id.ToString();
                    dashboardTicket.TrafficLight = GetTrafficLight(DateTime.Parse(dashboardTicket.TimeDue));
                    ticketList.Add(dashboardTicket);
                }
            }

            ticketList = QuickSortTickets(ticketList, 0, ticketList.Count - 1);

            return ticketList;
        }

        //Getting billable field
        private static bool GetBillableCustomField(object jObject)
        {
            var jsonString = JsonConvert.SerializeObject(jObject);
            var fieldDict = JObject.Parse(jsonString);
            return fieldDict["value"].ToObject<bool>();
        }

        //Finding user name from zendesk users api
        static string GetZendeskUserName(ZendeskUsers? zendeskUsers, Ticket ticket)
        {
            foreach (var user in zendeskUsers.users)
            {
                if (ticket.requester_id.ToString() == user.id.ToString())
                {
                    return user.name;
                }
            }
            return "null";
        }

        //Sort tickets on time due to finish
        public static List<DashboardTicketData> QuickSortTickets(List<DashboardTicketData> ticketListToSort, int leftIndex, int rightIndex)
        {
            var i = leftIndex;
            var j = rightIndex;
            var pivot = ticketListToSort[leftIndex];

            while (i <= j)
            {
                while (DateTime.Parse(ticketListToSort[i].TimeDue) < DateTime.Parse(pivot.TimeDue))
                {
                    i++;
                }

                while (DateTime.Parse(ticketListToSort[j].TimeDue) > DateTime.Parse(pivot.TimeDue))
                {
                    j--;
                }

                if (i <= j)
                {
                    (ticketListToSort[j], ticketListToSort[i]) = (ticketListToSort[i], ticketListToSort[j]); // swap using tuples
                    i++;
                    j--;
                }
            }

            if (leftIndex < j)
                QuickSortTickets(ticketListToSort, leftIndex, j);

            if (i < rightIndex)
                QuickSortTickets(ticketListToSort, i, rightIndex);

            return ticketListToSort;
        }

        //Creating time due based on user tier and ticket priority
        private static DateTime GetTimeDue(string organisation, string priority, DateTime requestedDate, IEnumerable<ConsoleUser.Models.Customer> customers, IEnumerable<ConsoleUser.Models.CustomerSupportLevel> supportLevel)
        {
            DateTime timeDue = DateTime.Now;
            foreach (var customer in customers)
            {
                if(customer.CustomerCode == organisation)
                {
                    int customerSupportLevel = customer.SupportLevel;

                    foreach (var level in supportLevel)
                    {
                        if(level.SupportLevel == customerSupportLevel)
                        {
                            var resolutionTime = priority == "urgent" ? level.ResolutionTimeUrgent : priority == "high" ? level.ResolutionTimeHigh : priority == "normal" ? level.ResolutionTimeNormal : level.ResolutionTimeLow;
                            timeDue = requestedDate.AddHours(resolutionTime);
                            //return requestedDate.AddHours(resolutionTime);
                        }
                    }
                }
            }
            
            return timeDue;
        }

        private static DateTime GetTimeDueMinusOffHours(string organisation, string priority, DateTime requestedDate, IEnumerable<ConsoleUser.Models.Customer> customers, IEnumerable<ConsoleUser.Models.CustomerSupportLevel> supportLevel)
        {
            //If ticket logged after hours, then log time changed to next working day morning.
            double resolutionTime = 0;
            double workHoursPerDay = 8;
            int dayStartHours = 8;
            int dayStartMinutes = 30;
            int dayEndHours = 17;
            int dayEndMinutes = 0;
            DateTime businessHoursEnd = new DateTime(requestedDate.Year, requestedDate.Month, requestedDate.Day, dayEndHours, dayEndMinutes, 0);
            DateTime nextBusinessDayStart = new DateTime(requestedDate.Year, requestedDate.Month, requestedDate.Day + 1, dayStartHours, dayStartMinutes, 0);

            // if next day is weekend find next Monday
            if (nextBusinessDayStart.DayOfWeek == DayOfWeek.Saturday) 
            {
                nextBusinessDayStart = nextBusinessDayStart.AddDays(2);
            }
            else if (nextBusinessDayStart.DayOfWeek == DayOfWeek.Sunday)
            {
                nextBusinessDayStart = nextBusinessDayStart.AddDays(1);
            }

            // If requested date is off hours or on weekend then requested date is the next working day.
            if (requestedDate.TimeOfDay > businessHoursEnd.TimeOfDay || 
                requestedDate.TimeOfDay < nextBusinessDayStart.TimeOfDay || 
                requestedDate.DayOfWeek == DayOfWeek.Saturday || 
                requestedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                requestedDate = nextBusinessDayStart;
            }

            //Finding resolution time based on customer tier and ticket priority
            foreach (var customer in customers)
            {
                if (customer.CustomerCode == organisation)
                {
                    int customerSupportLevel = customer.SupportLevel;

                    foreach (var level in supportLevel)
                    {
                        if (level.SupportLevel == customerSupportLevel)
                        {
                            resolutionTime = priority == "urgent" ? level.ResolutionTimeUrgent : priority == "high" ? level.ResolutionTimeHigh : priority == "normal" ? level.ResolutionTimeNormal : level.ResolutionTimeLow;
                        }
                    }
                }
            }

            //Adding off business hours to time due
            DateTime timeDue = requestedDate; //starting timeDue to ticket requested date
            businessHoursEnd = new DateTime(timeDue.Year, timeDue.Month, timeDue.Day, dayEndHours, dayEndMinutes, 0); // setting business hours end time for the current timeDue
            double timeLeftInDay = businessHoursEnd.Subtract(timeDue).TotalHours; // Balance time left in that day.

            if (resolutionTime <= timeLeftInDay) // If work can be finished in the same day.
            {
                timeDue = timeDue.AddHours(resolutionTime); // Adding response time to timeDue
                return timeDue;
            }

            // If cannot be completed in same day
            while (resolutionTime > timeLeftInDay) // Loop reducing 8 hrs until balance time is enough for the day
            {
                resolutionTime -= timeLeftInDay; // reducing work done for the day.
                timeLeftInDay = workHoursPerDay;  // Adding next days 8 hours.
                timeDue = new DateTime(timeDue.Year, timeDue.Month, timeDue.Day + 1, dayStartHours, dayStartMinutes, 0); // Adding a day to timeDue
                if (timeDue.DayOfWeek == DayOfWeek.Saturday)
                {
                    timeDue = timeDue.AddDays(2);
                }
            }

            timeDue = new DateTime(timeDue.Year, timeDue.Month, timeDue.Day + 1, dayStartHours, dayStartMinutes, 0); // Adding a day to timeDue
            // If added day is on saturday then skip to next Monday
            if (timeDue.DayOfWeek == DayOfWeek.Saturday)
            {
                timeDue = timeDue.AddDays(2);
            }
            timeDue = timeDue.AddHours(resolutionTime); // Adding balance time to 
            
            return timeDue;
        }

        //Generating colour codes based on 4 different time frames
        private static string GetTrafficLight(DateTime timeDue)
        {
            float veryHigh = 2;
            float high = 8;
            float normal = 24;
            float low = 48;
            double dueHours = (timeDue - DateTime.Now.ToLocalTime()).TotalHours;           
            return dueHours <= veryHigh ? "red" : dueHours <= high ? "orange" : dueHours <= normal ? "yellow" : "green"; 
        }
    }
}