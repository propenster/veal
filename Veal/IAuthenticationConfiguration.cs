using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Veal
{

    public interface IConfigurationOptions
    {
        HashSet<IAuthenticationConfiguration> AuthenticationConfigurations { get; set; }
        //HashSet<ISwaggerConfiguration> SwaggerConfigurations { get; set; }
    }
    public class ConfigurationOptions : IConfigurationOptions
    {
        public HashSet<IAuthenticationConfiguration> AuthenticationConfigurations { get; set;    } = new HashSet<IAuthenticationConfiguration>();
    }

    public interface IAuthenticationConfiguration : IConfigurationOptions
    {

    }
    public interface IJwtConfigurationOption : IAuthenticationConfiguration
    {
        bool ValidateIssuer { get; set; }
        bool ValidateAudience { get; set; }
        bool ValidateLifetime { get; set; }
        string ValidIssuer { get; set; }
        string ValidAudience { get; set; }
        TimeSpan ClockSkew { get; set; }
        SecurityKey IssuerSigningKey { get; set; }

    }
    public interface IBasicAuthenticationOption : IAuthenticationConfiguration
    {
        string ValidUsername { get; set; }
        string ValidPassword { get; set; }
    }
    public class BasicAuthenticationOption : IBasicAuthenticationOption
    {
        public string ValidUsername { get; set; }
        public string ValidPassword { get; set;  }
        public HashSet<IAuthenticationConfiguration> AuthenticationConfigurations { get; set; }
    }
    public class JwtConfigurationOption : IJwtConfigurationOption
    {
        public bool ValidateIssuer { get; set; }
        public bool ValidateAudience { get; set ; }
        public bool ValidateLifetime { get ; set ; }
        public string ValidIssuer { get; set; }
        public string ValidAudience { get; set; }
        public TimeSpan ClockSkew { get; set; }
        public SecurityKey IssuerSigningKey { get; set; }
        public HashSet<IAuthenticationConfiguration> AuthenticationConfigurations { get; set; }
        public bool ValidateIssuerSigningKey { get; set; }
    }
}
