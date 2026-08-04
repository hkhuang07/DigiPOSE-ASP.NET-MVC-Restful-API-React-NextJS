namespace DigiPOSE.Models
{
    public class TurnstileSettings
    {
        public const string SectionName = "CloudflareTurnstile";

        public bool IsEnabled { get; set; } = false;
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string VerificationUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    }
}
