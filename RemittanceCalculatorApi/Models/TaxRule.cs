public record TaxRule(
    string SourceCountry,
    string TargetCountry,
    decimal FlatTransferFee,
    decimal InterbankMarkupPercent,
    decimal LocalTaxPercent
);
