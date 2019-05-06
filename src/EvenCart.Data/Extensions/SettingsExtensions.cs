﻿using System.Collections.Generic;
using System.Security.Policy;
using EvenCart.Data.Entity.Settings;

namespace EvenCart.Data.Extensions
{
    public static class SettingsExtensions
    {
        public static string GetUrlProtocol(this UrlSettings urlSettings)
        {
            return urlSettings.EnableSsl ? "https" : "http";
        }

        public static IList<string> GetBannedIps(this SecuritySettings securitySettings)
        {
            return securitySettings.BannedIps?.Split(';');
        }

        public static IList<string> GetAdminRestrictedIps(this SecuritySettings securitySettings)
        {
            return securitySettings.AdminRestrictedIps?.Split(';');
        }
    }
}