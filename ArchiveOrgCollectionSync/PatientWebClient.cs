namespace ArchiveOrgCollectionSync
{
    using System;
    using System.Net;

    public class PatientWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = (int)TimeSpan.FromMinutes(60).TotalMilliseconds;
            return request;
        }
    }
}