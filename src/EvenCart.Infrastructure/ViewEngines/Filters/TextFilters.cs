﻿using System;
using DotLiquid;
using EvenCart.Core;
using EvenCart.Core.Infrastructure;
using EvenCart.Data.Entity.Settings;
using EvenCart.Data.Helpers;
using EvenCart.Services.Serializers;
using EvenCart.Infrastructure.Extensions;
using EvenCart.Infrastructure.Localization;
using EvenCart.Services.Helpers;

namespace EvenCart.Infrastructure.ViewEngines.Filters
{
    public static class TextFilters
    {
        public static string T(string input)
        {
            var localizer = DependencyResolver.Resolve<ILocalizer>();
            return localizer.Localize(input);
        }

        public static string Pluralize(Context context, int input, string singular, string plural)
        {
            return string.Concat($"{input} ", T(input == 1 ? singular : plural));
        }

        public static string WithCurrency(Context context, decimal input, string currency = null)
        {
            return currency == null ? input.ToCurrency() : input.ToCurrency(currency);
        }

        public static string NewLine2Br(string input)
        {
            return input.Replace(Environment.NewLine, "<br/>");
        }

        public static string ScriptJson(Context context, object input, string variableName)
        {
            var serializer = DependencyResolver.Resolve<IDataSerializer>();
            var json = serializer.Serialize(input);
            return $"<script type='text/javascript'>var {variableName}={json};</script>";
        }

        public static string AbsoluteUrl(string input)
        {
            var generalSettings = DependencyResolver.Resolve<GeneralSettings>();
            return WebHelper.GetUrlFromPath(input, generalSettings.StoreDomain);
        }

        public static string StripHtml(string input)
        {
            return HtmlUtility.StripHtml(input);
        }

        public static string Pretty(DateTime date)
        {
            return DateTimeHelper.GetRelativeDate(date);
        }
    }
}