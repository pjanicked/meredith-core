﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WhyNotEarth.Meredith.BrowTricks.Models;
using WhyNotEarth.Meredith.Data.Entity;
using WhyNotEarth.Meredith.Data.Entity.Models;
using WhyNotEarth.Meredith.Data.Entity.Models.Modules.BrowTricks;
using WhyNotEarth.Meredith.Exceptions;
using WhyNotEarth.Meredith.Identity;
using WhyNotEarth.Meredith.Models;
using WhyNotEarth.Meredith.Tenant;

namespace WhyNotEarth.Meredith.BrowTricks
{
    internal class ClientService : IClientService
    {
        private readonly MeredithDbContext _dbContext;
        private readonly TenantService _tenantService;
        private readonly IUserService _userService;

        public ClientService(IUserService userService, MeredithDbContext dbContext, TenantService tenantService)
        {
            _userService = userService;
            _dbContext = dbContext;
            _tenantService = tenantService;
        }

        public async Task CreateAsync(string tenantSlug, ClientModel model, User user)
        {
            var tenant = await _tenantService.CheckPermissionAsync(user, tenantSlug);

            var client = await MapClientAsync(new Client(), model, tenant);

            _dbContext.Clients.Add(client);
            await _dbContext.SaveChangesAsync();
        }

        public async Task EditAsync(int clientId, ClientModel model, User user)
        {
            var client = await GetClientAsync(user, clientId);

            client = await MapClientAsync(client, model);

            _dbContext.Clients.Update(client);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<Client>> GetListAsync(string tenantSlug, User user)
        {
            var tenant = await _tenantService.CheckPermissionAsync(user, tenantSlug);

            return await _dbContext.Clients
                .Include(item => item.User)
                .Where(item => item.TenantId == tenant.Id && item.IsArchived == false)
                .ToListAsync();
        }

        public async Task ArchiveAsync(int clientId, User user)
        {
            var client = await GetClientAsync(user, clientId);

            client.IsArchived = true;

            _dbContext.Clients.Update(client);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SetPmuAsync(int clientId, ClientPmuModel model, User user)
        {
            var client = await GetClientAsync(user, clientId);

            var questions = await _dbContext.PmuQuestions
                .Where(item => item.TenantId == client.TenantId)
                .ToListAsync();

            client = MapPmu(client, model, questions);

            _dbContext.Clients.Update(client);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<User> GetOrCreateUserAsync(ClientModel model)
        {
            var user = await _userService.GetUserAsync(model.Email);

            if (user != null)
            {
                return user;
            }

            var userCreateResult = await _userService.CreateAsync(new RegisterModel
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber
            });

            if (!userCreateResult.IdentityResult.Succeeded)
            {
                throw new InvalidActionException(userCreateResult.IdentityResult.Errors);
            }

            return userCreateResult.User!;
        }

        private async Task<Client> MapClientAsync(Client client, ClientModel model,
            Data.Entity.Models.Tenant? tenant = null)
        {
            if (client.User is null)
            {
                client.User = await GetOrCreateUserAsync(model);
            }
            else
            {
                client.User.Email = model.Email;
                client.User.FirstName = model.FirstName;
                client.User.LastName = model.LastName;
                client.User.PhoneNumber = model.PhoneNumber;
            }

            if (client.Tenant is null)
            {
                client.Tenant = tenant!;
            }

            client.NotificationType = model.NotificationTypes.ToFlag();

            return client;
        }

        public async Task<Client> GetClientAsync(User user, int clientId)
        {
            var client = await _dbContext.Clients
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.Id == clientId);

            if (client is null)
            {
                throw new RecordNotFoundException($"client {clientId} not found");
            }

            await _tenantService.CheckPermissionAsync(user, client.TenantId);

            return client;
        }

        private Client MapPmu(Client client, ClientPmuModel model, List<PmuQuestion> questions)
        {
            client.IsPmuCompleted = true;
            client.Signature = model.Signature;
            client.Initials = model.Initials;
            client.AllowPhoto = model.AllowPhoto;
            client.IsUnderCareOfPhysician = model.IsUnderCareOfPhysician;
            client.Conditions = model.Conditions;
            client.IsTakingBloodThinner = model.IsTakingBloodThinner;
            client.PhysicianName = model.PhysicianName;
            client.PhysicianPhoneNumber = model.PhysicianPhoneNumber;

            client.PmuAnswers = new List<PmuAnswer>();
            foreach (var pmuQuestion in questions)
            {
                var answer = model.Answers.FirstOrDefault(item => item.QuestionId == pmuQuestion.Id);

                if (answer is null)
                {
                    throw new InvalidActionException($"Question {pmuQuestion.Id} is not answered");
                }

                client.PmuAnswers.Add(new PmuAnswer
                {
                    QuestionId = pmuQuestion.Id,
                    Answer = answer.Answer
                });
            }

            return client;
        }
    }
}