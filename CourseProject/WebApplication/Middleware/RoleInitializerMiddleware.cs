﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Data;
using WebApplication.Models;

namespace WebApplication.Middleware
{
    public class RoleInitializerMiddleware
    {
        private readonly RequestDelegate _next;
        public RoleInitializerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            if (!(context.Session.Keys.Contains("roleStarting")))
            {
                await RoleInitializer.InitializeAsync(userManager, roleManager);
                context.Session.SetString("roleStarting", "Yes");
            }

            await _next(context);
        }
    }

    public static class RoleInitializerExtensions
    {
        public static IApplicationBuilder UseRoleInitializer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RoleInitializerMiddleware>();
        }

    }
}
