﻿using System.Collections.Generic;
using EvenCart.Data.Entity.Cultures;
using EvenCart.Data.Entity.Pages;
using EvenCart.Data.Entity.Users;
using EvenCart.Infrastructure.Helpers;
using EvenCart.Infrastructure.Mvc.Breadcrumbs;
using EvenCart.Infrastructure.Routing;
using Microsoft.AspNetCore.Http;

namespace EvenCart.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        private const string CurrentUserKey = "CurrentUser";
        private const string BreadcrumbKey = "BreadcrumbKey";
        private const string CurrentLanguageKey = "CurrentLanguageKey";
        private const string CurrentCurrencyKey = "CurrentCurrencyKey";
        private const string RequestSeoMetaKey = "RequestSeoMetaKey";

        public static void SetCurrentCurrency(this HttpContext httpContext, Currency currency)
        {
            httpContext.Items[CurrentCurrencyKey] = currency;
        }

        public static Currency GetCurrentCurrency(this HttpContext httpContext)
        {
            return (Currency)httpContext.Items[CurrentCurrencyKey];
        }

        public static void SetCurrentUser(this HttpContext httpContext, User user)
        {
            httpContext.Items[CurrentUserKey] = user;
        }

        public static User GetCurrentUser(this HttpContext httpContext)
        {
            return (User)httpContext.Items[CurrentUserKey];
        }

        public static void AppendToBreadcrumb(this HttpContext httpContext, BreadcrumbNode node)
        {
            var nodes = (List<BreadcrumbNode>)httpContext.Items[BreadcrumbKey];
            if (nodes == null)
            {
                nodes = new List<BreadcrumbNode>()
                {
                    new BreadcrumbNode()
                    {
                        DisplayText = LocalizationHelper.Localize("Home", ApplicationEngine.CurrentLanguageCultureCode),
                        Url = ApplicationEngine.RouteUrl(RouteNames.Home)
                    }
                };
                httpContext.Items[BreadcrumbKey] = nodes;
            }
            nodes.Add(node);
        }

        public static List<BreadcrumbNode> GetBreadcrumb(this HttpContext httpContext)
        {
            var nodes = (List<BreadcrumbNode>)httpContext.Items[BreadcrumbKey];
            return nodes;
        }

        public static void SetRequestSeoMeta(this HttpContext httpContext, SeoMeta seoMeta)
        {
            httpContext.Items[RequestSeoMetaKey] = seoMeta;
        }

        public static SeoMeta GetRequestSeoMeta(this HttpContext httpContext)
        {
            return (SeoMeta)httpContext.Items[RequestSeoMetaKey];
        }
    }
}