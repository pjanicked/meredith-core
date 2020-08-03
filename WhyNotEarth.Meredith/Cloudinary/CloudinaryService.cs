﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using WhyNotEarth.Meredith.Cloudinary.Models;
using WhyNotEarth.Meredith.Data.Entity.Models;
using Video = WhyNotEarth.Meredith.Data.Entity.Models.Video;

namespace WhyNotEarth.Meredith.Cloudinary
{
    internal class CloudinaryService : ICloudinaryService
    {
        private readonly CloudinaryOptions _options;

        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            _options = options.Value;
        }

        public Task DeleteByUrlAsync(string imageUrl)
        {
            // TODO: This is not safe, we should not do this
            var publicId = Path.GetFileNameWithoutExtension(imageUrl);

            return DeleteAsync(publicId);
        }

        public Task DeleteAsync(string publicId)
        {
            var cloudinary = new CloudinaryDotNet.Cloudinary(new Account(_options.CloudName,
                _options.ApiKey, _options.ApiSecret));

            var deleteParams = new DeletionParams(publicId);
            return cloudinary.DestroyAsync(deleteParams);
        }

        public async Task<Image?> GetUpdatedValueAsync(Image? oldValue, CloudinaryImageModel? model)
        {
            if (oldValue is null)
            {
                if (model != null)
                {
                    // Add
                    return new Image
                    {
                        Url = model.Url,
                        CloudinaryPublicId = model.PublicId
                    };
                }

                return null;
            }

            if (model != null)
            {
                // Update

                // Delete old image
                await DeleteAsync(oldValue.CloudinaryPublicId!);

                return new Image
                {
                    Id = oldValue.Id,
                    Url = model.Url,
                    CloudinaryPublicId = model.PublicId
                };
            }

            // Delete
            await DeleteAsync(oldValue.CloudinaryPublicId!);

            return null;
        }

        public async Task<List<Image>> GetUpdatedValueAsync(List<Image>? oldValues, List<CloudinaryImageModel>? models)
        {
            // Delete the removed ones from Cloudinary
            if (oldValues != null)
            {
                foreach (var oldValue in oldValues)
                {
                    if (!models.Any(item => item.PublicId == oldValue.CloudinaryPublicId))
                    {
                        await DeleteAsync(oldValue.CloudinaryPublicId!);
                    }
                }
            }

            return models?.Select(m =>
                new Image
                {
                    Id = oldValues?.FirstOrDefault(image => image.CloudinaryPublicId == m.PublicId)?.Id ??
                         default,
                    CloudinaryPublicId = m.PublicId,
                    Url = m.Url
                }).ToList() ?? new List<Image>();
        }

        public async Task<List<Video>> GetUpdatedValueAsync(List<Video>? oldValues, List<CloudinaryVideoModel>? models)
        {
            // Delete the removed ones from Cloudinary
            if (oldValues != null)
            {
                foreach (var oldValue in oldValues)
                {
                    if (!models.Any(item => item.PublicId == oldValue.CloudinaryPublicId))
                    {
                        await DeleteAsync(oldValue.CloudinaryPublicId!);
                    }
                }
            }

            return models?.Select(m =>
                new Video
                {
                    Id = oldValues?.FirstOrDefault(image => image.CloudinaryPublicId == m.PublicId)?.Id ??
                         default,
                    CloudinaryPublicId = m.PublicId,
                    Url = m.Url
                }).ToList() ?? new List<Video>();
        }
    }
}