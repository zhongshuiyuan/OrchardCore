using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.DisplayManagement.Liquid;

namespace OrchardCore.Liquid.Services
{
    public class LiquidTemplateManager : ILiquidTemplateManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceProvider _serviceProvider;

        private TemplateContext _context;

        public LiquidTemplateManager(
            IMemoryCache memoryCache,
            IServiceProvider serviceProvider)
        {
            _memoryCache = memoryCache;
            _serviceProvider = serviceProvider;
        }

        public TemplateContext Context => _context ??= LiquidViewTemplate.Context;

        public Task<string> RenderAsync(string source, TextEncoder encoder, object model)
        {
            if (String.IsNullOrWhiteSpace(source))
            {
                return Task.FromResult((string)null);
            }

            var result = GetCachedTemplate(source);

            return result.RenderAsync(_serviceProvider, encoder, Context, model);
        }

        private LiquidViewTemplate GetCachedTemplate(string source)
        {
            var errors = Enumerable.Empty<string>();

            var result = _memoryCache.GetOrCreate(source, (ICacheEntry e) =>
            {
                if (!LiquidViewTemplate.TryParse(source, out var parsed, out errors))
                {
                    // If the source string cannot be parsed, create a template that contains the parser errors
                    LiquidViewTemplate.TryParse(String.Join(System.Environment.NewLine, errors), out parsed, out errors);
                }

                // Define a default sliding expiration to prevent the 
                // cache from being filled and still apply some micro-caching
                // in case the template is use commonly
                e.SetSlidingExpiration(TimeSpan.FromSeconds(30));
                return parsed;
            });

            return result;
        }

        public bool Validate(string template, out IEnumerable<string> errors)
        {
            return LiquidViewTemplate.TryParse(template, out _, out errors);
        }
    }
}
