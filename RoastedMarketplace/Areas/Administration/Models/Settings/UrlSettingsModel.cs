﻿namespace RoastedMarketplace.Areas.Administration.Models.Settings
{
    public class UrlSettingsModel : SettingsModel
    {
        public string ProductUrlTemplate { get; set; }

        public string CategoryUrlTemplate { get; set; }

        public string ContentPageUrlTemplate { get; set; }
    }
}