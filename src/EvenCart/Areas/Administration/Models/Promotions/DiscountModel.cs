﻿using System;
using System.Collections.Generic;
using EvenCart.Data.Entity.Promotions;
using EvenCart.Infrastructure.Mvc.Models;
using EvenCart.Infrastructure.Mvc.Validator;
using FluentValidation;

namespace EvenCart.Areas.Administration.Models.Promotions
{
    public class DiscountModel : FoundationEntityModel, IRequiresValidations<DiscountModel>
    {
        public string Name { get; set; }

        public bool HasCouponCode { get; set; }

        public string CouponCode { get; set; }

        public int NumberOfTimesPerUser { get; set; }

        public int TotalNumberOfTimes { get; set; }

        public decimal MaximumDiscountAmount { get; set; }

        public CalculationType CalculationType { get; set; }

        public string CalculationTypeDisplay => CalculationType.ToString();

        public decimal DiscountValue { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool ExcludeAlreadyDiscountedProducts { get; set; }

        public bool Expired { get; set; }

        public bool Enabled { get; set; }

        public RestrictionType RestrictionType { get; set; }

        public decimal MinimumOrderSubTotal { get; set; }

        public string RestrictionTypeDisplay => RestrictionType.ToString();

        public IList<RestrictionValueModel> RestrictionValues { get; set; }

        public void SetupValidationRules(ModelValidator<DiscountModel> v)
        {
            v.RuleFor(x => x.Name).NotEmpty();
        }
    }
}