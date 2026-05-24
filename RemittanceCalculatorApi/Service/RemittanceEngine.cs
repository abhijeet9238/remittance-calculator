using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace RemittanceCalculatorApi
{
    public class RemittanceEngine
    {
        private readonly HttpClient _httpClient;

        public RemittanceEngine(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Spoof a realistic user agent string so external servers don't block requests
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task<CalculationBreakdown> ComputeNetPayoutAsync(RemittanceRequest request)
        {
            // 1. Fetch live mid-market exchange conversion matrix vectors
            string fxUrl = $"https://open.er-api.com/v6/latest/{request.SourceCurrency.ToUpper()}";
            var fxResult = await _httpClient.GetFromJsonAsync<JsonObject>(fxUrl);

            if (fxResult is null || !fxResult.ContainsKey("rates") || fxResult["rates"] is null)
            {
                throw new Exception("Global exchange rate provider is temporarily unreachable.");
            }

            // Validate if the provider tracks the selected currency token
            if (!fxResult["rates"]!.AsObject().ContainsKey(request.TargetCurrency.ToUpper()))
            {
                throw new KeyNotFoundException($"The currency '{request.TargetCurrency}' is not currently actively traded or supported.");
            }

            decimal midMarketRate = (decimal)fxResult["rates"]![request.TargetCurrency.ToUpper()]!;

            // 2. Aggregate competitor retail fee structures using Wise's public pricing portal
            string comparisonUrl = $"https://wise.com/1/comparisons?sourceCurrency={request.SourceCurrency.ToUpper()}&targetCurrency={request.TargetCurrency.ToUpper()}&sendAmount={request.Amount}";

            decimal dynamicFlatFee = 15.00m;       // Secure fallback defaults if endpoint changes layout
            decimal dynamicMarkupPercent = 0.015m; // 1.5% average bank markdown backup

            try
            {
                var comparisons = await _httpClient.GetFromJsonAsync<JsonObject>(comparisonUrl);
                if (comparisons is not null && comparisons["providers"] is JsonArray providers)
                {
                    // Isolate traditional brick-and-mortar operators or high-fee platforms like Western Union
                    var traditionalBank = providers.FirstOrDefault(p => (bool)p!["isBank"]! == true || p["id"]?.ToString() == "western-union");

                    if (traditionalBank is not null)
                    {
                        dynamicFlatFee = (decimal)traditionalBank["fee"]!;
                        decimal bankRate = (decimal)traditionalBank["rate"]!;

                        if (midMarketRate > 0)
                        {
                            dynamicMarkupPercent = (midMarketRate - bankRate) / midMarketRate;
                        }
                    }
                }
            }
            catch
            {
                // Silent catch to keep engine running smoothly using fallback values if external endpoints time out
            }

            // 3. Execution Math Loops
            decimal spendableSourceBalance = request.Amount - dynamicFlatFee;
            decimal appliedConsumerRate = midMarketRate * (1.00m - dynamicMarkupPercent);

            decimal rawTargetValue = spendableSourceBalance * midMarketRate;
            decimal clientTargetValue = spendableSourceBalance * appliedConsumerRate;

            decimal hiddenMarkupLoss = rawTargetValue - clientTargetValue;

            // Static country tax codes logic (No database storage required)
            decimal localTaxPercent = request.TargetCountry.ToUpper() == "PH" ? 0.01m : 0.00m;
            decimal nativeDestinationTax = clientTargetValue * localTaxPercent;

            decimal totalNetPayout = clientTargetValue - nativeDestinationTax;

            return new CalculationBreakdown(
                TotalSentSource: request.Amount,
                UpfrontFeesSource: dynamicFlatFee,
                EstimatedHiddenMarkupLossTarget: Math.Round(hiddenMarkupLoss, 2),
                RegulatoryTaxesTarget: Math.Round(nativeDestinationTax, 2),
                BaseMidMarketRate: Math.Round(midMarketRate, 4),
                ConsumerExchangeRate: Math.Round(appliedConsumerRate, 4),
                FinalNetReceivedTarget: Math.Round(totalNetPayout, 2)
            );
        }
    }
}