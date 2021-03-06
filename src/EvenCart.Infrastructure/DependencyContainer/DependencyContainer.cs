﻿#region License
// Copyright (c) Sojatia Infocrafts Private Limited.
// The following code is part of EvenCart eCommerce Software (https://evencart.co) Dual Licensed under the terms of
// 
// 1. GNU GPLv3 with additional terms (available to read at https://evencart.co/license)
// 2. EvenCart Proprietary License (available to read at https://evencart.co/license/commercial-license).
// 
// You can select one of the above two licenses according to your requirements. The usage of this code is
// subject to the terms of the license chosen by you.
#endregion

using System;
using System.Linq;
using System.Reflection;
using DryIoc;
using EvenCart.Core.Caching;
using EvenCart.Core.Config;
using EvenCart.Core.Infrastructure;
using EvenCart.Core.Infrastructure.Extensions;
using EvenCart.Core.Infrastructure.Interceptor;
using EvenCart.Core.Infrastructure.Providers;
using EvenCart.Core.Infrastructure.Utils;
using EvenCart.Core.Plugins;
using EvenCart.Core.Services.Events;
using EvenCart.Core.Services.Interceptor;
using EvenCart.Core.Tasks;
using EvenCart.Data.Database;
using EvenCart.Services.Authentication;
using EvenCart.Services.Emails;
using EvenCart.Services.Search;
using EvenCart.Services.Settings;
using EvenCart.Services.Users;
using EvenCart.Infrastructure.Authentication;
using EvenCart.Infrastructure.Bundle;
using EvenCart.Infrastructure.Caching;
using EvenCart.Infrastructure.Config;
using EvenCart.Infrastructure.Database;
using EvenCart.Infrastructure.DataTransfer;
using EvenCart.Infrastructure.Emails;
using EvenCart.Infrastructure.Localization;
using EvenCart.Infrastructure.MediaServices;
using EvenCart.Infrastructure.Mvc;
using EvenCart.Infrastructure.Mvc.Components;
using EvenCart.Infrastructure.Mvc.ModelFactories;
using EvenCart.Infrastructure.Plugins;
using EvenCart.Infrastructure.Routing;
using EvenCart.Infrastructure.Routing.Parsers;
using EvenCart.Infrastructure.Social;
using EvenCart.Infrastructure.Theming;
using EvenCart.Infrastructure.ViewEngines;
using EvenCart.Services.Cultures;
using EvenCart.Services.Formatter;

namespace EvenCart.Infrastructure.DependencyContainer
{
    public class DependencyContainer : IDependencyContainer
    {
        public void RegisterDependencies(IRegistrator registrar)
        {
            var asm = AssemblyLoader.GetAppDomainAssemblies();
            var allTypes = asm.Where(x => !x.IsDynamic).SelectMany(x =>
            {
                try
                {
                    return x.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return new Type[0];
                }
            })
                .Where(x => x.IsPublic && !x.IsAbstract).ToList();

            // settings register for access across app
            registrar.Register<IDatabaseSettings, DatabaseSettings>(reuse: Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Keep);
            registrar.Register<IApplicationConfiguration, ApplicationConfiguration>(reuse: Reuse.Singleton);

            //caching
            var cacheProviders = allTypes
                .Where(type => type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(ICacheProvider))));// which implementing some interface(s)
            //all providers which are not interfaces
            registrar.RegisterMany<ICacheProvider>(cacheProviders, type => type.FullName, Reuse.Singleton);

            //registrar.Register<ICacheProvider, RedisCacheProvider>(reuse: Reuse.Singleton);
            registrar.Register<ICacheAccountant, CacheAccountant>(reuse: Reuse.Transient);
            //events
            registrar.Register<IEventPublisherService, EventPublisherService>(reuse: Reuse.Singleton);
            //file access
            registrar.Register<ILocalFileProvider, LocalFileProvider>(reuse: Reuse.Singleton);
            //localizer
            registrar.Register<ILocalizer, Localizer>(reuse: Reuse.ScopedOrSingleton);
            //view engine & friends
            registrar.Register<IViewAccountant, ViewAccountant>(reuse: Reuse.Singleton);
            registrar.Register<IAppViewEngine, DefaultAppViewEngine>(reuse: Reuse.Singleton);
            //media
            registrar.Register<IImageProcessor, ImageProcessor>(reuse: Reuse.Singleton);
            registrar.Register<IMediaAccountant, MediaAccountant>(reuse: Reuse.Singleton);
            //plugin loader
            registrar.Register<IPluginAccountant, PluginAccountant>(reuse: Reuse.ScopedOrSingleton);
            //model mapper
            registrar.Register<IModelMapper, ModelMapper>(reuse: Reuse.Singleton);
            //routetemplate parser
            registrar.Register<IRouteTemplateParser, RouteTemplateParser>(reuse: Reuse.Singleton);
            registrar.Register<IDynamicRouteProvider, DynamicRouteProvider>(reuse: Reuse.ScopedOrSingleton);
            //themes
            registrar.Register<IThemeProvider, ThemeProvider>(reuse: Reuse.Singleton);
            //view compoenent
            registrar.Register<IViewComponentManager, ViewComponentManager>(reuse: Reuse.ScopedOrSingleton);
            //search query
            registrar.Register<ISearchQueryParserService, SearchQueryParserService>(reuse: Reuse.Scoped);
            //html processor
            registrar.Register<IHtmlProcessor, HtmlProcessor>(reuse: Reuse.Singleton);
            //email sender
            registrar.Register<IEmailSender, EmailSender>(reuse: Reuse.Transient);
            //bundler
            registrar.Register<IBundleService, BundleService>(reuse: Reuse.Transient);
            //minifier
            registrar.Register<IMinifier, Minifier>(reuse: Reuse.Transient);
            //interceptor
            registrar.Register<IInterceptorService, InterceptorService>(reuse: Reuse.Singleton);
            //conection accountant
            registrar.Register<IConnectionAccountant, ConnectionAccountant>(Reuse.Transient);


            //find all the model factories
            var allModelFactories = allTypes
                .Where(type => type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(IModelFactory))));// which implementing some interface(s)
            //all consumers which are not interfaces
            registrar.RegisterMany(allModelFactories);

            //capability providers
            var allCapabilityProviderTypes = allTypes
                .Where(type => type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(ICapabilityProvider))));// which implementing some interface(s)
            //all providers which are not interfaces
            registrar.RegisterMany(allCapabilityProviderTypes);

            //tasks
            var allTaskTypes = allTypes
                .Where(type => type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(ITask))));// which implementing some interface(s)
            //all providers which are not interfaces
            registrar.RegisterMany<ITask>(allTaskTypes, type => type.FullName);

            //interceptors
            var allInterceptorTypes = allTypes
                .Where(type => type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(IInterceptor))));// which implementing some interface(s)
            //all providers which are not interfaces
            registrar.RegisterMany<IInterceptor>(allInterceptorTypes, type => type.FullName);

            //currency providers
            var allCurrencyProviders = allTypes
                .Where(type => type.IsPublic && // get public types 
                               type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(ICurrencyRateProvider))));// which implementing some interface(s)
            //all providers which are not interfaces
            registrar.RegisterMany<ICurrencyRateProvider>(allCurrencyProviders, type => type.FullName);

            //currency providers
            var dataTransferProviders = allTypes
                .Where(type => type.IsPublic && // get public types 
                               type.GetInterfaces()
                                   .Any(x => x.IsAssignableTo(typeof(IDataTransferProvider))));// which implementing some interface(s)
            //all providers which are not interfaces
            registrar.RegisterMany<IDataTransferProvider>(dataTransferProviders, type => type.FullName, Reuse.Transient);
            registrar.Register<IDataTransferManager, DataTransferManager>();

            //services
            //to register services, we need to get all types from services assembly and register each of them;
            var serviceAssembly = asm.First(x => x.FullName.Contains("EvenCart.Services,"));
            var serviceTypes = serviceAssembly.GetTypes().
                Where(type => type.IsPublic && // get public types 
                              !type.IsAbstract && // which are not interfaces nor abstract
                              type.GetInterfaces().Length != 0);// which implementing some interface(s)

            registrar.RegisterMany(serviceTypes, Reuse.Transient);

            registrar.Register<IFormatterService, FormatterService>(Reuse.Singleton,
                ifAlreadyRegistered: IfAlreadyRegistered.Replace);

            //find all event consumer types
            var allConsumerTypes = allTypes
                .Where(type => type.GetInterfaces()
                    .Any(x => x.IsAssignableTo(typeof(IFoundationEvent))));// which implementing some interface(s)
            //all consumers which are not interfaces
            registrar.RegisterMany(allConsumerTypes);

            //components
            //find all event consumer types
            var allComponents = allTypes
                .Where(type => type.IsClass && type.IsAssignableTo(typeof(FoundationComponent)));// which implementing some interface(s)

            registrar.RegisterMany(allComponents, Reuse.Transient);

            //settings
            var allSettingTypes = TypeFinder.ClassesOfType<ISettingGroup>();
            foreach (var settingType in allSettingTypes)
            {
                var type = settingType;
                registrar.Use(type, context => SettingsFactory.GetSetting(type));
                //registrar.RegisterDelegate(type, resolver =>
                //{
                //    var x = resolver.Resolve<ISettingService>().GetSettings(type, ApplicationEngine.CurrentStore?.Id ?? 0);
                //    return x;
                //}, reuse: Reuse.Transient);

            }

            registrar.Register<IAppAuthenticationService, AuthenticationService>(reuse: Reuse.Transient);

            var allModules = TypeFinder.ClassesOfType<IPlugin>();
            foreach (var moduleType in allModules)
            {
                var type = moduleType;
                registrar.Register(type, reuse: Reuse.Singleton);
            }
        }

        public void RegisterDependenciesIfActive(IRegistrator registrar)
        {

        }

        public int Priority { get; }
    }
}