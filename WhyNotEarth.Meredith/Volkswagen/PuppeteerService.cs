﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;
using WhyNotEarth.Meredith.Data.Entity.Models.Modules.Volkswagen;

namespace WhyNotEarth.Meredith.Volkswagen
{
    public class PuppeteerService
    {
        private readonly JumpStartEmailTemplateService _jumpStartEmailTemplateService;

        public PuppeteerService(JumpStartEmailTemplateService jumpStartEmailTemplateService)
        {
            _jumpStartEmailTemplateService = jumpStartEmailTemplateService;
        }

        public async Task<byte[]> BuildPdfAsync(JumpStart jumpStart)
        {
            await using var browser = await GetBrowser();

            await using var page = await browser.NewPageAsync();

            var emailTemplate = _jumpStartEmailTemplateService.GetPdfHtml(jumpStart);

            await page.SetContentAsync(emailTemplate);

            var pdfData = await page.PdfDataAsync(new PdfOptions
            {
                PrintBackground = true
            });

            return pdfData;
        }

        public async Task<byte[]> BuildScreenshotAsync(JumpStart jumpStart)
        {
            await using var browser = await GetBrowser();

            await using var page = await browser.NewPageAsync();

            var emailTemplate = _jumpStartEmailTemplateService.GetEmailHtml(jumpStart);

            await page.SetContentAsync(emailTemplate);

            var screenShotData = await page.ScreenshotDataAsync(new ScreenshotOptions
            {
                FullPage = true
            });

            return screenShotData;
        }

        private async Task<Browser> GetBrowser()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
        }
    }
}