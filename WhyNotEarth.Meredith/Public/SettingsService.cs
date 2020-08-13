﻿using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace WhyNotEarth.Meredith.Public
{
    public class SettingsService
    {
        private readonly IDbContext _dbContext;

        public SettingsService(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SetValueAsync<T>(string companySlug, T value)
        {
            var json = JsonConvert.SerializeObject(value);

            var config = await _dbContext.Settings
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Company.Slug == companySlug.ToLower());

            if (config is null)
            {
                var company = await _dbContext.Companies
                    .FirstOrDefaultAsync(item => item.Slug == companySlug.ToLower());

                _dbContext.Settings.Add(new Setting
                {
                    CompanyId = company.Id,
                    Value = json
                });
            }
            else
            {
                config.Value = json;
                _dbContext.Settings.Update(config);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<T> GetValueAsync<T>(string companySlug) where T : new()
        {
            var config = await _dbContext.Settings
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Company.Slug == companySlug.ToLower());

            if (config?.Value == null)
            {
                return new T();
            }

            var value = JsonConvert.DeserializeObject<T>(config.Value);

            return value;
        }
    }
}