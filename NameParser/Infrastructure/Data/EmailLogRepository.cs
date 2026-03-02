using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NameParser.Infrastructure.Data.Models;

namespace NameParser.Infrastructure.Data
{
    public class EmailLogRepository
    {
        public void LogEmail(string emailType, int? challengeId, string recipientEmail, string recipientName, 
            string subject, bool isSuccess, string errorMessage = null, bool isTest = false)
        {
            using var context = new RaceManagementContext();
            
            var emailLog = new EmailLogEntity
            {
                EmailType = emailType,
                ChallengeId = challengeId,
                RecipientEmail = recipientEmail,
                RecipientName = recipientName,
                Subject = subject,
                SentDate = DateTime.Now,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                IsTest = isTest,
                SentBy = Environment.UserName
            };

            context.EmailLogs.Add(emailLog);
            context.SaveChanges();
        }

        public List<EmailLogEntity> GetEmailLogsByChallenge(int challengeId, bool includeTests = false)
        {
            using var context = new RaceManagementContext();
            
            var query = context.EmailLogs
                .Where(e => e.ChallengeId == challengeId && e.EmailType == "Challenge");

            if (!includeTests)
            {
                query = query.Where(e => !e.IsTest);
            }

            return query.OrderByDescending(e => e.SentDate).ToList();
        }

        public List<EmailLogEntity> GetEmailLogsByType(string emailType, bool includeTests = false)
        {
            using var context = new RaceManagementContext();
            
            var query = context.EmailLogs.Where(e => e.EmailType == emailType);

            if (!includeTests)
            {
                query = query.Where(e => !e.IsTest);
            }

            return query.OrderByDescending(e => e.SentDate).ToList();
        }

        public EmailLogEntity GetLastEmailLog(string recipientEmail, string emailType, int? challengeId = null)
        {
            using var context = new RaceManagementContext();
            
            var query = context.EmailLogs
                .Where(e => e.RecipientEmail == recipientEmail && e.EmailType == emailType && !e.IsTest);

            if (challengeId.HasValue)
            {
                query = query.Where(e => e.ChallengeId == challengeId);
            }

            return query.OrderByDescending(e => e.SentDate).FirstOrDefault();
        }

        public List<EmailLogEntity> GetRecentEmailLogs(int daysBack = 7)
        {
            using var context = new RaceManagementContext();
            
            var cutoffDate = DateTime.Now.AddDays(-daysBack);
            return context.EmailLogs
                .Where(e => e.SentDate >= cutoffDate)
                .OrderByDescending(e => e.SentDate)
                .ToList();
        }

        public void DeleteOldLogs(int daysToKeep = 90)
        {
            using var context = new RaceManagementContext();
            
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var oldLogs = context.EmailLogs.Where(e => e.SentDate < cutoffDate);
            context.EmailLogs.RemoveRange(oldLogs);
            context.SaveChanges();
        }
    }
}
