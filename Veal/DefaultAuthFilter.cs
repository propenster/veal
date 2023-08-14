using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Veal
{
    internal class DefaultAuthFilter : IAuthorizationFilter
    {
        public HttpResponder OnAuthentication(ActionExecutingContext context, string scheme)
        {
            context.Request.Headers.TryGetValue("Authorization", out var tokenHeader);
            if (string.IsNullOrWhiteSpace(tokenHeader))
            {
                context.Result = HttpResponder.Unauthorized();
                return context.Result;
            }

            if(scheme.ToUpperInvariant() == "BEARER")
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    //fetch already configured JwtOptions from HttpAppServer...
                    var configuredJwtOptions = (JwtConfigurationOption)context.App.ConfigurationOptions.AuthenticationConfigurations.FirstOrDefault(c => typeof(IJwtConfigurationOption).IsAssignableFrom(c.GetType()));
                    if(configuredJwtOptions == null)
                    {
                        context.Result = HttpResponder.Unauthorized();
                        return context.Result;
                    }

                    tokenHandler.ValidateToken(tokenHeader.Split(' ').LastOrDefault(), new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = configuredJwtOptions.ValidateIssuerSigningKey,
                        IssuerSigningKey = configuredJwtOptions.IssuerSigningKey,
                        ValidateIssuer = configuredJwtOptions.ValidateIssuer,
                        ValidateAudience = configuredJwtOptions.ValidateAudience,
                        ValidateLifetime = configuredJwtOptions.ValidateLifetime,
                        // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                        ClockSkew = configuredJwtOptions.ClockSkew  == null ? TimeSpan.Zero : configuredJwtOptions.ClockSkew,

                    }, out SecurityToken validatedToken);

                    var jwtToken = (JwtSecurityToken)validatedToken;
                }
                catch (Exception ex)
                {

                    context.Result = HttpResponder.Unauthorized();
                    return context.Result;
                }
            }else if(scheme.ToUpperInvariant() == "BASIC")
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    //fetch already configured JwtOptions from HttpAppServer...
                    var configuredBasicAuthOptions = (BasicAuthenticationOption)context.App.ConfigurationOptions.AuthenticationConfigurations.FirstOrDefault(c => typeof(IBasicAuthenticationOption).IsAssignableFrom(c.GetType()));
                    if (configuredBasicAuthOptions == null)
                    {
                        context.Result = HttpResponder.Unauthorized();
                        return context.Result;
                    }
                    var token = tokenHeader.Split(' ').LastOrDefault();
                    var valueBytes = Convert.FromBase64String(token);

                    var rawString = Encoding.UTF8.GetString(valueBytes).Split(':');
                    var authSuccess = rawString.First() == configuredBasicAuthOptions.ValidUsername && rawString.Last() == configuredBasicAuthOptions.ValidPassword;
                    if (!authSuccess)
                    {
                        context.Result = HttpResponder.Unauthorized();
                        return context.Result;
                    }
                }
                catch (Exception ex)
                {

                    context.Result = HttpResponder.Unauthorized();
                    return context.Result;
                }
            }


            return context.Result;


        }
    }
}
