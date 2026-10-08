using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace backend.Model
{
    /// <summary>
    /// Multiple output binding for <c>IngestShortLink</c>. <see cref="Entity"/> is only
    /// written to the table when it is set, which lets the function reject invalid or
    /// unauthenticated requests with a proper status code instead of storing them.
    /// </summary>
    public class IngestShortLinkOutput
    {
        [TableOutput("shorturls", Connection = "AzureStorageConnection")]
        public ShortUrl Entity { get; set; }

        [HttpResult]
        public IActionResult Result { get; set; }
    }
}
