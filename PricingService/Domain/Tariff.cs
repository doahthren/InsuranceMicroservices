#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PricingService.Domain
{
    public class BasePremiumRule
    {
        public BasePremiumRule(string coverCode, string applyIf, string priceFormula)
        {
            CoverCode = coverCode;
            ApplyIf = applyIf;
            PriceFormula = priceFormula;
        }

        public string CoverCode { get; }
        public string ApplyIf { get; }
        public string PriceFormula { get; }

        public bool AppliesTo(string coverCode, IDictionary<string, object> subject)
        {
            if (CoverCode != coverCode)
                return false;
            return string.IsNullOrWhiteSpace(ApplyIf) || ExpressionEvaluator.EvaluateBool(ApplyIf, subject);
        }

        public decimal Calculate(IDictionary<string, object> subject)
        {
            return ExpressionEvaluator.EvaluateDecimal(PriceFormula, subject);
        }
    }

    public class BasePremiumRuleSet
    {
        private readonly List<BasePremiumRule> rules = new List<BasePremiumRule>();

        public void AddBasePriceRule(string coverCode, string applyIf, string priceFormula)
        {
            rules.Add(new BasePremiumRule(coverCode, applyIf, priceFormula));
        }

        public decimal CalculateBasePrice(string coverCode, IDictionary<string, object> subject)
        {
            var rule = rules.FirstOrDefault(r => r.AppliesTo(coverCode, subject));
            if (rule == null)
                throw new InvalidOperationException("No pricing rule found for cover '" + coverCode + "'.");
            return rule.Calculate(subject);
        }
    }

    public class DiscountMarkupRule
    {
        public DiscountMarkupRule(string applyIf, decimal factor)
        {
            ApplyIf = applyIf;
            Factor = factor;
        }

        public string ApplyIf { get; }
        public decimal Factor { get; }

        public bool AppliesTo(IDictionary<string, object> subject)
        {
            return string.IsNullOrWhiteSpace(ApplyIf) || ExpressionEvaluator.EvaluateBool(ApplyIf, subject);
        }
    }

    public class DiscountMarkupRuleSet
    {
        private readonly List<DiscountMarkupRule> rules = new List<DiscountMarkupRule>();

        public void AddPercentMarkup(string applyIf, decimal factor)
        {
            rules.Add(new DiscountMarkupRule(applyIf, factor));
        }

        public decimal CalculateFactor(IDictionary<string, object> subject)
        {
            var factor = 1M;
            foreach (var rule in rules)
                if (rule.AppliesTo(subject))
                    factor = factor * rule.Factor;
            return factor;
        }
    }

    public class Tariff
    {
        public Tariff(string code)
        {
            Code = code;
            BasePremiumRules = new BasePremiumRuleSet();
            DiscountMarkupRules = new DiscountMarkupRuleSet();
        }

        public string Code { get; }
        public BasePremiumRuleSet BasePremiumRules { get; }
        public DiscountMarkupRuleSet DiscountMarkupRules { get; }

        public Price CalculatePrice(Calculation calculation)
        {
            var factor = DiscountMarkupRules.CalculateFactor(calculation.Subject);
            var covers = new Dictionary<string, CoverPrice>();

            foreach (var coverCode in calculation.SelectedCovers)
            {
                var basePrice = BasePremiumRules.CalculateBasePrice(coverCode, calculation.Subject);
                var price = decimal.Round(basePrice * factor, 2, MidpointRounding.AwayFromZero);
                covers[coverCode] = new CoverPrice(coverCode, price);
            }

            return new Price(covers);
        }
    }
}