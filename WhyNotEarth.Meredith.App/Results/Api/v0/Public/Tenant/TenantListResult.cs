﻿using System;
using System.Collections.Generic;

namespace WhyNotEarth.Meredith.App.Results.Api.v0.Public.Tenant
{
    public class TenantListResult
    {
        public string Slug { get; }

        public string Name { get; }

        public string? Logo { get; }

        public List<string>? Tags { get; }

        public TimeSpan DeliveryTime { get; }

        public decimal DeliveryFee { get; }

        public string? Description { get; }

        public bool IsActive { get; }

        public TenantListResult(Meredith.Public.Tenant tenant)
        {
            Slug = tenant.Slug;
            Name = tenant.Name;
            Logo = tenant.Logo?.Url;
            Tags = tenant.Tags;
            DeliveryTime = tenant.DeliveryTime;
            DeliveryFee = tenant.DeliveryFee;
            Description = tenant.Description;
            IsActive = tenant.IsActive;
        }
    }
}