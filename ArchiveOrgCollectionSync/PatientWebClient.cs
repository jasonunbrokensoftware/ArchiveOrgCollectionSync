namespace ArchiveOrgCollectionSync
{
    using System;
    using System.Net;

    public class PatientWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            if (request != null)
            {
                request.Timeout = (int)TimeSpan.FromMinutes(60).TotalMilliseconds;
            }

            return request;
        }
    }
}