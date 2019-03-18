﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RoastedMarketplace.Data.Entity.Purchases;
using RoastedMarketplace.Data.Entity.Reviews;
using RoastedMarketplace.Data.Entity.Settings;
using RoastedMarketplace.Data.Entity.Shop;
using RoastedMarketplace.Data.Extensions;
using RoastedMarketplace.Infrastructure;
using RoastedMarketplace.Infrastructure.MediaServices;
using RoastedMarketplace.Infrastructure.Mvc;
using RoastedMarketplace.Infrastructure.Mvc.Attributes;
using RoastedMarketplace.Infrastructure.Mvc.ModelFactories;
using RoastedMarketplace.Infrastructure.Routing;
using RoastedMarketplace.Models.Media;
using RoastedMarketplace.Models.Products;
using RoastedMarketplace.Models.Reviews;
using RoastedMarketplace.Services.Extensions;
using RoastedMarketplace.Services.Products;
using RoastedMarketplace.Services.Purchases;
using RoastedMarketplace.Services.Reviews;

namespace RoastedMarketplace.Controllers
{
    public class ReviewsController : FoundationController
    {
        private readonly IReviewService _reviewService;
        private readonly IOrderService _orderService;
        private readonly CatalogSettings _catalogSettings;
        private readonly IModelMapper _modelMapper;
        private readonly IProductService _productService;
        private readonly IMediaAccountant _mediaAccountant;
        public ReviewsController(IReviewService reviewService, IOrderService orderService, CatalogSettings catalogSettings, IModelMapper modelMapper, IProductService productService, IMediaAccountant mediaAccountant)
        {
            _reviewService = reviewService;
            _orderService = orderService;
            _catalogSettings = catalogSettings;
            _modelMapper = modelMapper;
            _productService = productService;
            _mediaAccountant = mediaAccountant;
        }

        [ValidateModelState(ModelType = typeof(ReviewModel))]
        [DualPost("reviews", Name = RouteNames.SaveReview, OnlyApi = true)]
        public IActionResult SaveReview(ReviewModel reviewModel)
        {
            var userId = ApplicationEngine.CurrentUser.Id;
            bool verifiedPurchase = false;
            if (_catalogSettings.AllowReviewsForStorePurchaseOnly)
            {
                //do we have a valid order?
                var order = _orderService.Get(reviewModel.OrderId);
                if (order == null || order.OrderItems.All(x => x.ProductId != reviewModel.ProductId))
                    return R.Fail.With("error", T("Invalid order details provided")).Result;

                if (order.OrderStatus != OrderStatus.Complete)
                    return R.Fail.With("error", T("The order is not available for review")).Result;
                //do we already have a review for this order?
                var savedReview = _reviewService.FirstOrDefault(
                    x => x.OrderId == reviewModel.OrderId && x.ProductId == reviewModel.ProductId &&
                         x.UserId == userId);

                if (savedReview != null)
                {
                    return R.Fail.With("error", T("You've already reviewed this product item")).Result;
                }

                verifiedPurchase = true;

            }
            else
            {
                //is this a valid product?
                var product = _productService.Get(reviewModel.ProductId);
                if (!product.IsPublic())
                    return R.Fail.With("error", T("Invalid order details provided")).Result;

            }
            //create a new review
            var review = _modelMapper.Map<Review>(reviewModel);
            review.CreatedOn = DateTime.UtcNow;
            review.UpdatedOn = DateTime.UtcNow;
            review.VerifiedPurchase = verifiedPurchase;
            review.Published = !_catalogSettings.EnableReviewModeration;
            review.UserId = userId;
            _reviewService.Insert(review);
            return R.Success.Result;
        }

        [DynamicRoute(Name = RouteNames.ReviewsList, SeoEntityName = nameof(Product), SettingName = nameof(UrlSettings.ProductUrlTemplate), TemplateSuffix = "/reviews", ParameterName = nameof(ReviewSearchModel.ProductId))]
        public IActionResult ReviewsList(ReviewSearchModel reviewSearchModel)
        {
            return ReviewsListApi(reviewSearchModel);
        }

        [DualGet("reviews/{productId}", Name = RouteNames.ReviewsList, OnlyApi = true)]
        public IActionResult ReviewsListApi(ReviewSearchModel reviewSearchModel)
        {
            //check if the product is valid
            var product = _productService.Get(reviewSearchModel.ProductId);
            if (!product.IsPublic())
                return NotFound();

            IList<Review> reviews;
            var ratings = reviewSearchModel.Rating.HasValue
                ? new List<int>() {reviewSearchModel.Rating.Value}
                : new List<int>() { 1, 2, 3, 4, 5 };
            int totalMatches;
            if (reviewSearchModel.VerifiedPurchase)
                reviews = _reviewService.Get(out totalMatches,
                    x => x.ProductId == reviewSearchModel.ProductId && ratings.Contains(x.Rating) &&
                         x.VerifiedPurchase == true && x.Published, page: reviewSearchModel.Page,
                    count: reviewSearchModel.Count).ToList();
            else
                reviews = _reviewService.Get(out totalMatches,
                    x => x.ProductId == reviewSearchModel.ProductId && ratings.Contains(x.Rating) && x.Published,
                    page: reviewSearchModel.Page,
                    count: reviewSearchModel.Count).ToList();

            var reviewModels = reviews.Select(GetReviewModel).ToList();

            var reviewsSummaryModel = new AllReviewsSummaryModel()
            {
                TotalReviews = totalMatches
            };
            //find best and worst review
            ReviewModel bestReview = null, worstReview = null;
            if (reviews.Count > 1 && reviews.Count < reviewSearchModel.Count)
            {
                bestReview = reviewModels.OrderByDescending(x => x.Rating).First();
                worstReview = reviewModels.OrderBy(x => x.Rating).First();
                reviewsSummaryModel.FiveStarCount = reviews.Count(x => x.Rating == 5);
                reviewsSummaryModel.FourStarCount = reviews.Count(x => x.Rating == 4);
                reviewsSummaryModel.ThreeStarCount = reviews.Count(x => x.Rating == 3);
                reviewsSummaryModel.TwoStarCount = reviews.Count(x => x.Rating == 2);
                reviewsSummaryModel.OneStarCount = reviews.Count(x => x.Rating == 1);
            }
            else
            {
                if (reviews.Count > 1)
                {
                    bestReview = GetReviewModel(_reviewService.GetBestReview(product.Id));
                    worstReview = GetReviewModel(_reviewService.GetWorstReview(product.Id));
                    reviewsSummaryModel.FiveStarCount = _reviewService.Count(x => x.Rating == 5 && x.ProductId == product.Id && x.Published);
                    reviewsSummaryModel.FourStarCount = _reviewService.Count(x => x.Rating == 4 && x.ProductId == product.Id && x.Published);
                    reviewsSummaryModel.ThreeStarCount = _reviewService.Count(x => x.Rating == 3 && x.ProductId == product.Id && x.Published);
                    reviewsSummaryModel.TwoStarCount = _reviewService.Count(x => x.Rating == 2 && x.ProductId == product.Id && x.Published);
                    reviewsSummaryModel.OneStarCount = _reviewService.Count(x => x.Rating == 1 && x.ProductId == product.Id && x.Published);
                }
            }
            var productModel = _modelMapper.Map<ProductModel>(product);
            productModel.SeName = product.SeoMeta.Slug;
            var mediaModels = product.MediaItems?.Select(y =>
            {
                var mediaModel = _modelMapper.Map<MediaModel>(y);
                mediaModel.ThumbnailUrl =
                    _mediaAccountant.GetPictureUrl(y, ApplicationEngine.ActiveTheme.ProductBoxImageSize, true);
                mediaModel.Url = _mediaAccountant.GetPictureUrl(y, ApplicationEngine.ActiveTheme.ProductBoxImageSize, true);
                return mediaModel;
            }).ToList();
            productModel.Media = mediaModels;

            return R.Success
                .With("product", productModel)
                .With("reviews", reviewModels)
                .With("bestReview", bestReview)
                .With("worstReview", worstReview)
                .WithGridResponse(totalMatches, reviewSearchModel.Page, reviewSearchModel.Count)
                .With("summary", reviewsSummaryModel).Result;
        }

        private ReviewModel GetReviewModel(Review review)
        {
            var model = _modelMapper.Map<ReviewModel>(review);
            model.DisplayName = review.Private ? _catalogSettings.DisplayNameForPrivateReviews : review.User.Name;
            if (model.DisplayName.IsNullEmptyOrWhiteSpace())
            {
                model.DisplayName = T("Store Customer");
            }
            return model;
        }
    }
}