using System.Linq;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        var _logger = this.Context.Logger;
        _logger.LogInformation($"Operation ID: {this.Context.OperationId}");
        
        switch (this.Context.OperationId)
        {
            case "GetDirectDownloadUrl":
                return await this.HandleGetDirectDownloadUrl().ConfigureAwait(false);
            case "GetServiceTags":
                return await this.HandleGetServiceTags().ConfigureAwait(false);
            case "GetAllFileContents":
                return await this.HandleGetAllFileContents().ConfigureAwait(false);
            case "GetIPAddressesByServiceTag":
                return await this.HandleGetIPAddressesByServiceTag().ConfigureAwait(false);
            case "CIDRReducer":
                return await this.HandleCIDRReducer().ConfigureAwait(false);
            case "GenerateIPRules":
                return await this.HandleGenerateIPRules().ConfigureAwait(false);
            default:
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
                return response;
        }
    }
    
    // New function for GetDirectDownloadUrl action
    private async Task<HttpResponseMessage> HandleGetDirectDownloadUrl()
    {
        
        // Update query string parameter "id" using the provided example approach
        var locationUriBuilder = new UriBuilder(this.Context.Request.RequestUri);
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        // Operation: Extract string parameter 'id' from request (pseudo-code)
        string idString = query["id"]; // ...existing or custom implementation
        query["id"] = ConvertIdParameter(idString);
        locationUriBuilder.Query = query.ToString();
        this.Context.Request.RequestUri = locationUriBuilder.Uri;
        
        HttpResponseMessage response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);        
        var extractedUrl = ExtractDownloadUrl(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        // Pass a JSON string to CreateJsonContent
        response.Content =  new StringContent("{\"downloadUrl\": \"" + extractedUrl + "\"}", Encoding.UTF8, "application/json");
        return response;
    }
    
    private async Task<HttpResponseMessage> HandleGetServiceTags()
    {
        // Update query string parameter "downloadUrl" using the provided example approach
        var locationUriBuilder = new UriBuilder(this.Context.Request.RequestUri);
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        // Extract string parameter 'downloadUrl' from request
        string downloadUrl = query["downloadUrl"];
        this.Context.Request.RequestUri = new Uri(downloadUrl);
        
        // Send the request to get the file content
        HttpResponseMessage serviceResponse = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(false);
        var content = await serviceResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        
        string resultJson = GetServiceTags(content);
        
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(resultJson, System.Text.Encoding.UTF8, "application/json");
        return response;
    }    
    
    // New function for GetAllFileContents action
    private async Task<HttpResponseMessage> HandleGetAllFileContents()
    {
        // Update query string parameter "downloadUrl" using the provided example approach
        var locationUriBuilder = new UriBuilder(this.Context.Request.RequestUri);
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        // Extract string parameter 'downloadUrl'
        string downloadUrl = query["downloadUrl"];
        this.Context.Request.RequestUri = new Uri(downloadUrl);
        
        // Send the request to get the file content
        HttpResponseMessage serviceResponse = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(false);
        
        // Return the file content inside a JSON object with property "downloadUrl"
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        return response;
    }
    
    // Updated GetIPAddressesByServiceTag action to accept multiple service tags from the body.
    private async Task<HttpResponseMessage> HandleGetIPAddressesByServiceTag()
    {
        var _logger = this.Context.Logger;
        // Extract remaining query parameters.
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        string downloadUrl = query["downloadUrl"];
        string ipVersion = query["ipVersion"];
        if (string.IsNullOrEmpty(ipVersion))
        {
            ipVersion = "IPv4";
        }
        
        // Read the request body to get the array of service tags.
        string bodyContent = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        // Updated conversion: first parse to JArray then convert to string array.
        _logger.LogInformation($"Body content: {bodyContent}");
        JArray jsonArray = JArray.Parse(bodyContent);
        var serviceTags = jsonArray.ToObject<string[]>();
        
        // Set the request URI to the downloadUrl.
        this.Context.Request.RequestUri = new Uri(downloadUrl);
        this.Context.Request.Method = HttpMethod.Get;        

        // Send the request to get the file content.
        HttpResponseMessage serviceResponse = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(false);
        string content = await serviceResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        
        // Parse the JSON content to extract IP addresses for any of the specified service tags.
        var jObject = JObject.Parse(content);
        var values = jObject["values"];
        var ipList = new List<string>();
        
        foreach (var token in values)
        {
            string tagName = token["name"]?.ToString();
            if (serviceTags != null && serviceTags.Contains(tagName))
            {
                var addressPrefixes = token["properties"]["addressPrefixes"] as JArray;
                if (addressPrefixes != null)
                {
                    foreach (var ipPrefix in addressPrefixes)
                    {
                        string ipPrefixStr = ipPrefix.ToString();
                        string version = DetermineIPVersion(ipPrefixStr);
                        if (ipVersion == "All" || version == ipVersion)
                        {
                            ipList.Add(ipPrefixStr);
                        }
                    }
                }
            }
        }
        
        var ipArray = new JArray(ipList);
        var result = new JObject(
            new JProperty("ipAddresses", ipArray),
            new JProperty("ipCount", ipList.Count)
        );
        string resultJson = result.ToString();
        
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(resultJson, System.Text.Encoding.UTF8, "application/json");
        return response;
    }
    
    // New function for CIDRReducer action
    private async Task<HttpResponseMessage> HandleCIDRReducer()
    {
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var cidrJArray = JArray.Parse(contentAsString);
        var cidrArray = cidrJArray.ToObject<string[]>();

        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);       // Parse maxPrefix; default to 32 if not provided
        int maxPrefix = 32;
        int.TryParse(query["maxPrefix"], out maxPrefix);
        
        // Reduce the CIDR list
        var reduced = ReduceCIDRs(cidrArray, maxPrefix);
        int originalCount = cidrArray.Length;
        int reducedCount = reduced.Length;
        
        // Build output JSON with additional properties
        var output = new JObject(
            new JProperty("reducedValues", new JArray(reduced)),
            new JProperty("originalCount", originalCount),
            new JProperty("reducedCount", reducedCount)
        );
        string resultJson = output.ToString();
        
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(resultJson, System.Text.Encoding.UTF8, "application/json");
        return response;
    }
    
    // New function for Generate IP Rules action
    private async Task<HttpResponseMessage> HandleGenerateIPRules()
    {
        // Retrieve the "action" query parameter; default to "Allow"
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        string ruleAction = query["action"];
        if (string.IsNullOrEmpty(ruleAction))
        {
            ruleAction = "Allow";
        }
        
        // Read the request body which contains a JSON array of IP addresses/CIDRs
        string bodyContent = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var ipListJArray = JArray.Parse(bodyContent);
        var ipListArray = ipListJArray.ToObject<string[]>();        
        
        // Build the JSON array of IP rules
        var rulesArray = new JArray(
            ipListArray.Select(ip => new JObject(
                new JProperty("value", ip),
                new JProperty("action", ruleAction)
            ))
        );
        string resultJson = rulesArray.ToString();
        
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(resultJson, System.Text.Encoding.UTF8, "application/json");
        return response;
    }
    
    // Operation: Convert string parameter value into integer based on mapping
    private string ConvertIdParameter(string idStr)
    {
        switch(idStr)
        {
            case "Public Cloud":
                return "56519";
            case "US Government":
                return "57063";
            case "China Cloud":
                return "57062";
            default:
                // Handle error or default case
                return "0";
        }
    }

    private string ExtractDownloadUrl(string htmlContent)
    {
       var regex = new Regex("<a\\s+href=[\"'](https://download\\.microsoft\\.com/download/[^\"']+)[\"']", RegexOptions.IgnoreCase);
        var match = regex.Match(htmlContent);

        return match.Success ? match.Groups[1].Value : null;
    }

    // New function: returns a string array of all the Service Tag names from the provided JSON content.
    private string GetServiceTags(string jsonContent)
    {
        var jObject = JObject.Parse(jsonContent);
        var values = jObject["values"];
        if (values == null)
            return new JObject(new JProperty("serviceTags", new JArray())).ToString();
        var serviceTags = values.Select(token => token["name"]?.ToString() ?? string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
        var tagsArray = new JArray(serviceTags);
        var result = new JObject(new JProperty("serviceTags", tagsArray));
        return result.ToString();
    }

    // New function: DetermineIPVersion - returns "IPv4" or "IPv6" for a given IP address or CIDR string.
    private string DetermineIPVersion(string ipOrCIDR)
    {
        // If the string contains a slash, assume it's CIDR and extract the IP part.
        string ipPart = ipOrCIDR.Contains("/") ? ipOrCIDR.Split('/')[0] : ipOrCIDR;
        if (System.Net.IPAddress.TryParse(ipPart, out var ip))
        {
            return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "IPv4" :
                   ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? "IPv6" : "Unknown";
        }
        return "Unknown";
    }
    
    // Updated ReduceCIDRs: Remove CIDRs with prefix > maxPrefix, then eliminate those contained within another.
    private string[] ReduceCIDRs(string[] cidrValues, int maxPrefix)
    {
        var filteredList = new List<string>();
        // Filter out any CIDR with a prefix greater than maxPrefix.
        foreach(var cidr in cidrValues)
        {
            var parts = cidr.Trim().Split('/');
            if(parts.Length != 2) continue;
            if(!int.TryParse(parts[1], out int prefix)) continue;
            if(prefix > maxPrefix)
                continue; // Skip this CIDR.
            filteredList.Add(cidr.Trim());
        }
        
        // Remove duplicates.
        var distinctCidrs = filteredList.Distinct().ToList();
        
        // Filter out CIDRs that are contained within another.
        var result = new List<string>();
        for (int i = 0; i < distinctCidrs.Count; i++)
        {
            bool isContained = false;
            for (int j = 0; j < distinctCidrs.Count; j++)
            {
                if (i == j) continue;
                if (IsContainedIn(distinctCidrs[i], distinctCidrs[j]))
                {
                    isContained = true;
                    break;
                }
            }
            if (!isContained)
            {
                result.Add(distinctCidrs[i]);
            }
        }
        return result.ToArray();
    }

    // New helper function to check if cidrA is contained in cidrB (IPv4 only)
    private bool IsContainedIn(string cidrA, string cidrB)
    {
        // Split CIDRs and ensure valid format
        var partsA = cidrA.Split('/');
        var partsB = cidrB.Split('/');
        if (partsA.Length != 2 || partsB.Length != 2) return false;
        if (!int.TryParse(partsA[1], out int prefixA) || !int.TryParse(partsB[1], out int prefixB))
            return false;
        // Only consider containment if B represents a larger network
        if(prefixB >= prefixA)
            return false;
        if(!System.Net.IPAddress.TryParse(partsA[0], out System.Net.IPAddress ipA) ||
           !System.Net.IPAddress.TryParse(partsB[0], out System.Net.IPAddress ipB))
             return false;
        
        // Convert to UInt32 (big-endian) - works for IPv4 only
        uint ipUintA = ((uint)ipA.GetAddressBytes()[0] << 24) | ((uint)ipA.GetAddressBytes()[1] << 16) |
                       ((uint)ipA.GetAddressBytes()[2] << 8)  | ipA.GetAddressBytes()[3];
        uint ipUintB = ((uint)ipB.GetAddressBytes()[0] << 24) | ((uint)ipB.GetAddressBytes()[1] << 16) |
                       ((uint)ipB.GetAddressBytes()[2] << 8)  | ipB.GetAddressBytes()[3];
        // Generate mask for B
        uint mask = prefixB == 0 ? 0u : 0xFFFFFFFF << (32 - prefixB);
        // If A's network equals B's network, then A is contained in B.
        return (ipUintA & mask) == (ipUintB & mask);
    }    
}