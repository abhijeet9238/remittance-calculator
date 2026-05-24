
// --- DATA CONTRACTS ---
public record RemittanceRequest(
    string SourceCountry,   // e.g., "AE"
    string TargetCountry,   // e.g., "IN"
    string SourceCurrency,  // e.g., "AED"
    string TargetCurrency,  // e.g., "INR"
    decimal Amount
);
