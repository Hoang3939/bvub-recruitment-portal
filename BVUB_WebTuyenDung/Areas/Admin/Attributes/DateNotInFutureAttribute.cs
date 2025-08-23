using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BVUB_WebTuyenDung.Areas.Admin.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class DateNotInFutureAttribute : ValidationAttribute, IClientModelValidator
    {
        public DateNotInFutureAttribute()
        {
            // Thông báo mặc định
            ErrorMessage = "Ngày không hợp lệ.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is null) return ValidationResult.Success;

            if (value is DateTime d && d.Date <= DateTime.Today)
                return ValidationResult.Success;

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        // client-side (unobtrusive)
        public void AddValidation(ClientModelValidationContext context)
        {
            Merge(context.Attributes, "data-val", "true");
            Merge(context.Attributes, "data-val-datenotfuture", ErrorMessage);
        }

        private static void Merge(IDictionary<string, string> attrs, string key, string value)
        {
            if (!attrs.ContainsKey(key)) attrs.Add(key, value);
        }
    }
}
