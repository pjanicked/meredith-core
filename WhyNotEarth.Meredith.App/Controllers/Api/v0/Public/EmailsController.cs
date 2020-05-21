﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WhyNotEarth.Meredith.App.Results.Api.v0.Public.SendGrid;
using WhyNotEarth.Meredith.Data.Entity;
using WhyNotEarth.Meredith.Data.Entity.Models;

namespace WhyNotEarth.Meredith.App.Controllers.Api.v0.Public
{
    [ApiVersion("0")]
    [Route("api/v0/emails")]
    [ProducesErrorResponseType(typeof(void))]
    public class EmailsController : ControllerBase
    {
        private readonly MeredithDbContext _dbContext;

        public EmailsController(MeredithDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("sendgrid/webhook")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<OkResult> Create(List<SendGridEventItem> events)
        {
            foreach (var eventList in events.Batch(100))
            {
                foreach (var eventItem in eventList)
                {
                    await eventItem.Apply(_dbContext);
                }

                await _dbContext.SaveChangesAsync();
            }

            return Ok();
        }

        [Returns404]
        [HttpGet("{companySlug}/stats")]
        public async Task<ActionResult<EmailStatsResult>> Stats(string companySlug)
        {
            var company = await _dbContext.Companies.FirstOrDefaultAsync(item => item.Name == companySlug.ToLower());

            if (company is null)
            {
                return NotFound($"Company '{companySlug}' not found.");
            }

            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            var monthlySentEmails = await _dbContext.EmailRecipients.Where(item =>
                item.CreationDateTime >= lastMonth &&
                item.CompanyId == company.Id).CountAsync();

            var monthlyActiveUsers = await _dbContext.EmailRecipients
                .Where(item =>
                    item.Status >= EmailStatus.Opened &&
                    item.DeliverDateTime >= lastMonth)
                .Select(item => item.Email)
                .Distinct()
                .CountAsync();

            return Ok(new EmailStatsResult(monthlyActiveUsers, monthlySentEmails));
        }
    }

    public class SendGridEventItem
    {
        // Schema: https://sendgrid.com/docs/for-developers/tracking-events/event/

        [JsonProperty(nameof(EmailRecipient.MemoId))]
        public int? MemoId { get; set; }

        [JsonProperty(nameof(EmailRecipient.JumpStartId))]
        public int? JumpStartId { get; set; }

        [JsonProperty(nameof(EmailRecipient.CompanyId))]
        public int CompanyId { get; set; }

        public int Timestamp { get; set; }

        public string Email { get; set; } = null!;

        public string Event { get; set; } = null!;

        public EmailStatus Status =>
            Event switch
            {
                "delivered" => EmailStatus.Delivered,
                "open" => EmailStatus.Opened,
                "click" => EmailStatus.Clicked,
                _ => EmailStatus.None
            };

        public DateTime DateTime => DateTimeOffset.FromUnixTimeSeconds(Timestamp).UtcDateTime;

        public async Task Apply(MeredithDbContext dbContext)
        {
            Expression<Func<EmailRecipient, bool>> baseCondition = item =>
                item.CompanyId == CompanyId && item.Email == Email;

            Expression<Func<EmailRecipient, bool>> secondaryCondition;
            if (MemoId.HasValue)
            {
                secondaryCondition = item => item.MemoId == MemoId;
            }
            else if (JumpStartId.HasValue)
            {
                secondaryCondition = item => item.JumpStartId == MemoId;
            }
            else
            {
                return;
            }

            var condition =
                Expression.Lambda<Func<EmailRecipient, bool>>(Expression.AndAlso(baseCondition, secondaryCondition));

            var emailRecipient = await dbContext.EmailRecipients.FirstOrDefaultAsync(condition);

            if (emailRecipient is null)
            {
                return;
            }

            Update(emailRecipient);
        }

        private void Update(EmailRecipient emailRecipient)
        {
            if (Status == EmailStatus.Delivered)
            {
                emailRecipient.DeliverDateTime = DateTime;
            }
            else if (Status == EmailStatus.Opened)
            {
                emailRecipient.OpenDateTime = DateTime;
            }

            if (emailRecipient.Status < Status)
            {
                emailRecipient.Status = Status;
            }
        }
    }
}