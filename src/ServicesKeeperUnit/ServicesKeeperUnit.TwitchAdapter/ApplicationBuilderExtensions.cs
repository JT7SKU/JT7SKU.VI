using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServicesKeeperUnit.Twitch.Adapter.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServicesKeeperUnit.Twitch.Adapter
{
   public static class ApplicationBuilderExtensions
    {
        public static void UseTwitchAdapter(this IApplicationBuilder app)
        {
            var twitchOptions = app.ApplicationServices.GetRequiredService<IOptions<TwitchOptions>>().Value;
            var uriPath = new Uri(twitchOptions.ApitUrl);

            app.UseWhen(
                context => context.Request.Path.StartsWithSegments(uriPath.AbsolutePath),
                builder => builder.UseMiddleware<TwitchMiddleware>());
        }
    }
}
