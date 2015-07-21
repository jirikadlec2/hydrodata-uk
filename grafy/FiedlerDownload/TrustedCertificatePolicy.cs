namespace Fiedler
{
//====================================================================
// trustedCertificatePolicy.cs
//====================================================================
using System;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;

    public class trustedCertificatePolicy : System.Net.ICertificatePolicy
    {
        public trustedCertificatePolicy() {}

        public bool CheckValidationResult
        (
            System.Net.ServicePoint sp,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Net.WebRequest request, int problem)
        {
            return true;
        }
    }
}

