using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Coree.NETStandard.Abstractions.ServiceFactoryEx;
using System.Text.RegularExpressions;

namespace Coree.NETASP.Services.CertificateManager
{
    public partial class CertificateManagerService : ServiceFactoryEx<CertificateManagerService>, ICertificateManagerService
    {
        private readonly ILogger<CertificateManagerService>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateManagerService"/> class.
        /// </summary>
        /// <param name="logger">Optional logger instance for logging purposes.</param>
        /// <remarks>
        /// The logger provided here can be used with the field within the class.
        /// Be mindful that the logger may be null in scenarios where it's not explicitly provided.
        /// </remarks>
        public CertificateManagerService(ILogger<CertificateManagerService>? logger = null)
        {
            this._logger = logger;
        }
    }

    public interface ICertificateManagerService
    {
    }

    public class CommonDistinguishedNameBuilder
    {
        private readonly List<string> _attributes = new List<string>();
        private bool _isBuilt = false; // Flag to check if build has been invoked.

        public void AddAttribute(string key, string value)
        {
            if (_isBuilt)
                throw new InvalidOperationException("Cannot add attributes after the distinguished name is built.");
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

            _attributes.Add($"{key}={EscapeValue(value)}");
        }

        public void AddCommonName(string commonName) => AddAttribute("CN", commonName);
        public void AddOrganizationName(string organizationName) => AddAttribute("O", organizationName);
        public void AddOrganizationalUnitName(string organizationalUnitName) => AddAttribute("OU", organizationalUnitName);
        public void AddCountryCode(string countryCode) => AddAttribute("C", ValidateCountryCode(countryCode));
        public void AddLocalityName(string localityName) => AddAttribute("L", localityName);
        public void AddStateOrProvinceName(string stateOrProvinceName) => AddAttribute("ST", stateOrProvinceName);
        public void AddCountryOrRegion(string countryOrRegion) => AddAttribute("C", countryOrRegion);
        public void AddEmailAddress(string emailAddress) => AddAttribute("E", ValidateEmailAddress(emailAddress));

        /// <summary>
        /// Builds the distinguished name and prevents further modifications to this builder instance.
        /// </summary>
        /// <returns>An immutable distinguished name object.</returns>
        public DistinguishedName Build()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Distinguished name has already been built.");

            _isBuilt = true;
            return new DistinguishedName(string.Join(", ", _attributes));
        }

        private static string ValidateCountryCode(string countryCode)
        {
            if (countryCode.Length != 2 || !Regex.IsMatch(countryCode, "^[A-Za-z]{2}$"))
                throw new ArgumentException("Country code must be exactly two letters.", nameof(countryCode));
            return countryCode.ToUpper();
        }

        private static string ValidateEmailAddress(string emailAddress)
        {
            if (!Regex.IsMatch(emailAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Invalid email address format.", nameof(emailAddress));
            return emailAddress;
        }

        private static string EscapeValue(string value)
        {
            return value.Replace("+", "\\+")
                        .Replace(",", "\\,")
                        .Replace(";", "\\;")
                        .Replace("<", "\\<")
                        .Replace(">", "\\>")
                        .Replace("\"", "\\\"")
                        .Replace("\\", "\\\\");
        }
    }

    public class DistinguishedName
    {
        public string Name { get; }

        public DistinguishedName(string name)
        {
            Name = name;
        }
    }

    public static class CertificateManager
    {
        /// <summary>
        /// Generates or loads a self-signed X509 certificate.
        /// </summary>
        /// <param name="subjectName">The subject name for the certificate, typically a DNS name.</param>
        /// <param name="validityPeriodYears">The number of years the certificate will remain valid.</param>
        /// <param name="password">The password used to encrypt the PFX file.</param>
        /// <param name="fileName">The file name for the certificate PFX file.</param>
        /// <returns>An X509Certificate2 containing both the public key and associated private key.</returns>
        /// <remarks>
        /// This method checks for an existing certificate file and loads it if valid. If no valid certificate is found, a new one is generated.
        /// </remarks>
        public static X509Certificate2 GenerateAndOrLoadSelfSignedCertificate(string subjectName, string[] sanNames, int validityPeriodYears, string password, string fileName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            // Check if the certificate file exists and try to load it
            if (File.Exists(filePath))
            {
                try
                {
                    var cert = new X509Certificate2(filePath, password, X509KeyStorageFlags.UserKeySet);
                    // Check if the certificate is still valid
                    if (cert.NotAfter > DateTime.Now)
                    {
                        return cert; // Return the loaded certificate if it is valid
                    }
                }
                catch (CryptographicException)
                {
                    // Handle exceptions if the certificate file is corrupt or the password is incorrect
                    Console.WriteLine("Failed to load the existing certificate. A new one will be generated.");
                }
            }

            // Generate a new certificate if no valid existing certificate is found
            var newCert = GenerateSelfSignedCertificate2(subjectName, sanNames, validityPeriodYears, password);
            File.WriteAllBytes(filePath, newCert.Export(X509ContentType.Pfx, password)); // Save the new certificate to file
            return newCert;
        }

        public static X509Certificate2 GenerateSelfSignedCertificate2(string subjectName, string[] sanNames, int validityPeriodYears, string password)
        {
            CommonDistinguishedNameBuilder dnBuilder = new CommonDistinguishedNameBuilder();

            dnBuilder.AddCommonName(subjectName);
            dnBuilder.AddOrganizationName("Company");
            dnBuilder.AddOrganizationalUnitName("IT");
            dnBuilder.AddLocalityName("Redmond");
            dnBuilder.AddStateOrProvinceName("Washington");
            dnBuilder.AddCountryOrRegion("US");
            dnBuilder.AddEmailAddress("dontreply@example.com");

            var distinguishedName = dnBuilder.Build();



            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256); // Using NIST P-256 curve
            var req = new CertificateRequest($"{distinguishedName.Name}", ecdsa, HashAlgorithmName.SHA256);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));  // For TLS Web Server Authentication
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

            // Add SAN extension
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var san in sanNames)
            {
                sanBuilder.AddDnsName(san);
            }
            req.CertificateExtensions.Add(sanBuilder.Build());

            var cert = req.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddYears(validityPeriodYears)));

            var pfxBytes = cert.Export(X509ContentType.Pfx, password);
            return new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.Exportable);
        }
    }
}