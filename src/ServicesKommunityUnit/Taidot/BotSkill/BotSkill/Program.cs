// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BotSkill.Services;
using JaniC3N7.Yleinen.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BotSkill
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebbiRakentaja(args).Run();
        }

        public static WebApplication WebbiRakentaja(string[] args)
        {
            var rakentaja = WebApplication.CreateBuilder(args);
            rakentaja.Services.AddMvc();
            rakentaja.Services.AddSingleton<BotServices>();
            rakentaja.Services.AddControllers();
            rakentaja.Services.AddAgentAspNetAuthentication(rakentaja.Configuration);

            var app = rakentaja.Build();
            //app.MapDefaultEndpoints();

            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
           
    }
}
