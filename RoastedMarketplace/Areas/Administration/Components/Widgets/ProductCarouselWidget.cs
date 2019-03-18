﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RoastedMarketplace.Areas.Administration.Models.Media;
using RoastedMarketplace.Areas.Administration.Models.Shop;
using RoastedMarketplace.Core.Plugins;
using RoastedMarketplace.Data.Entity.Settings;
using RoastedMarketplace.Factories.Products;
using RoastedMarketplace.Infrastructure;
using RoastedMarketplace.Infrastructure.MediaServices;
using RoastedMarketplace.Infrastructure.Mvc;
using RoastedMarketplace.Infrastructure.Mvc.ModelFactories;
using RoastedMarketplace.Infrastructure.Mvc.Models;
using RoastedMarketplace.Models.Reviews;
using RoastedMarketplace.Services.Products;
using RoastedMarketplace.Services.Widgets;

namespace RoastedMarketplace.Areas.Administration.Components.Widgets
{
    [ViewComponent(Name = WidgetSystemName)]
    public class ProductCarouselWidget : FoundationComponent, IWidget
    {
        private const string WidgetSystemName = "ProductCarousel";
        private readonly IWidgetService _widgetService;
        private readonly IProductService _productService;
        private readonly IModelMapper _modelMapper;
        private readonly IMediaAccountant _mediaAccountant;
        private readonly IProductModelFactory _productModelFactory;
        public ProductCarouselWidget(IWidgetService widgetService, IProductService productService, IModelMapper modelMapper, IMediaAccountant mediaAccountant, IProductModelFactory productModelFactory)
        {
            _widgetService = widgetService;
            _productService = productService;
            _modelMapper = modelMapper;
            _mediaAccountant = mediaAccountant;
            _productModelFactory = productModelFactory;
        }

        public override IViewComponentResult Invoke(object data = null)
        {
            var dataAsDict = data as Dictionary<string, object>;
            if (dataAsDict == null)
                return R.Success.ComponentResult;
            var widgetId = dataAsDict["id"].ToString();
            var widgetSettings = _widgetService.LoadWidgetSettings<ProductCarouselWidgetSettings>(widgetId);
            if (!widgetSettings.ProductIds.Any())
                return R.Success.ComponentResult;
            var products = _productService.GetProducts(widgetSettings.ProductIds, true);
            var productsModel = products.Select(_productModelFactory.Create)
                .OrderBy(x => widgetSettings.ProductIds.IndexOf(x.Id))
                .ToList();

            return R.Success.With("title", widgetSettings.Title)
                .With("products", productsModel)
                .With("widgetId", widgetId)
                .ComponentResult;
        }

        public string DisplayName => "Product Carousel";

        public string SystemName => WidgetSystemName;

        public IList<string> WidgetZones { get; } = null;

        public bool HasConfiguration { get; } = true;

        public string ConfigurationUrl { get; } = null;

        public Type SettingsType { get; } = typeof(ProductCarouselWidgetSettings);

        public object GetViewObject(object settings)
        {
            var widgetSettings = settings as ProductCarouselWidgetSettings;
            if (widgetSettings?.ProductIds != null)
            {
                var products = _productService.GetProducts(widgetSettings.ProductIds);
                var productsModel = products.Select(x =>
                    {
                        var model = _modelMapper.Map<ProductModel>(x);
                        model.Media = x.MediaItems?.Select(y =>
                            {
                                var mediaModel = _modelMapper.Map<MediaModel>(y);
                                mediaModel.ThumbnailUrl = _mediaAccountant.GetPictureUrl(y, 100, 100);
                                return mediaModel;
                            })
                            .ToList();
                        return model;
                    })
                    .OrderBy(x => widgetSettings.ProductIds.IndexOf(x.Id))
                    .ToList();
                return new
                {
                    title = widgetSettings.Title,
                    products = productsModel
                };
            }
            return new
            {
                title = widgetSettings?.Title,
            };
        }

        public class ProductCarouselWidgetSettings : WidgetSettingsModel
        {
            public string Title { get; set; }

            public IList<int> ProductIds { get; set; }
        }
    }
}