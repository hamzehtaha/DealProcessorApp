using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealProcessor.Core.Models
{
    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }

        public static ValidationResult Success()
            => new() { IsValid = true };

        public static ValidationResult Failure(string errorMessage)
            => new() { IsValid = false, ErrorMessage = errorMessage };
    }
}
