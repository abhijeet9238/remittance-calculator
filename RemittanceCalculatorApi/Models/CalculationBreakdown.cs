public record CalculationBreakdown(
    decimal TotalSentSource,
    decimal UpfrontFeesSource,
    decimal EstimatedHiddenMarkupLossTarget,
    decimal RegulatoryTaxesTarget,
    decimal BaseMidMarketRate,
    decimal ConsumerExchangeRate,
    decimal FinalNetReceivedTarget
);
