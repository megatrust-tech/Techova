namespace taskedin_be.src.Modules.Auth.Options
{
    /// <summary>
    /// Strongly-typed JWT configuration options.
    /// Single source of truth for JWT settings used across the application.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Secret key for signing access tokens.
        /// </summary>
        public string AccessTokenSecret { get; set; } = string.Empty;

        /// <summary>
        /// Issuer claim (iss) - identifies who created and signed the token.
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Audience claim (aud) - identifies the intended recipient of the token.
        /// </summary>
        public string Audience { get; set; } = string.Empty;
    }

}
