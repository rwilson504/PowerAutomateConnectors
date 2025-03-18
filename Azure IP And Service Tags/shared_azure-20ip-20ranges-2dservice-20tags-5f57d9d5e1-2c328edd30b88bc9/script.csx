using System.Linq;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Check if the operation ID is GetDirectDownloadUrl
        if (this.Context.OperationId == "GetDirectDownloadUrl")
        {
            return await this.HandleGetDirectDownloadUrl().ConfigureAwait(false);
        }
        
        // Check if the operation ID is GetServiceTags
        if (this.Context.OperationId == "GetServiceTags")
        {
            return await this.HandleGetServiceTags().ConfigureAwait(false);
        }
        
        if (this.Context.OperationId == "GetAllFileContents")  // new branch
        {
            return await this.HandleGetAllFileContents().ConfigureAwait(false);
        }
        
        if (this.Context.OperationId == "GetIPAddressesByServiceTag")
        {
            return await this.HandleGetIPAddressesByServiceTag().ConfigureAwait(false);
        }
        
        if (this.Context.OperationId == "CIDRReducer")
        {
            return await this.HandleCIDRReducer().ConfigureAwait(false);
        }
        
        if (this.Context.OperationId == "GenerateIPRules")
        {
            return await this.HandleGenerateIPRules().ConfigureAwait(false);
        }
        
        // Handle an invalid operation ID
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        response.Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
        return response;
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
    
    // New function for GetIPAddressesByServiceTag action
    private async Task<HttpResponseMessage> HandleGetIPAddressesByServiceTag()
    {
        // Update query string and extract parameters
        var locationUriBuilder = new UriBuilder(this.Context.Request.RequestUri);
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        string downloadUrl = query["downloadUrl"];
        string serviceTag = query["serviceTag"];
        string ipVersion = query["ipVersion"];
        if (string.IsNullOrEmpty(ipVersion))
        {
            ipVersion = "IPv4";
        }
        
        // Set the request URI to the downloadUrl
        this.Context.Request.RequestUri = new Uri(downloadUrl);
        
        // Send the request to get the file content
        HttpResponseMessage serviceResponse = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(false);
        string content = await serviceResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        
        // Parse the JSON content to extract IP addresses for the specified service tag
        var jObject = JObject.Parse(content);
        var values = jObject["values"];
        var ipList = new List<string>();
        
        foreach (var token in values)
        {
            if (token["name"]?.ToString() == serviceTag)
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
        // Create output JSON with both ipAddresses and ipCount
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
    
    // New function: ReduceCIDRs - adjusts each CIDR if its prefix > maxPrefix and returns distinct list.
    private string[] ReduceCIDRs(string[] cidrValues, int maxPrefix)
    {
        var result = new List<string>();
        foreach(var cidr in cidrValues)
        {
            string adjusted = AdjustCIDR(cidr.Trim(), maxPrefix);
            result.Add(adjusted);
        }
        return result.Distinct().ToArray();
    }

    // New function: AdjustCIDR - for IPv4, if the CIDR's prefix is greater than maxPrefix, adjust it, else return unchanged.
    private string AdjustCIDR(string cidr, int maxPrefix)
    {
        var parts = cidr.Split('/');
        if(parts.Length != 2)
            return cidr;
        if(!int.TryParse(parts[1], out int prefix))
            return cidr;
        // Only adjust if IPv4 and prefix is larger than maxPrefix.
        if(prefix <= maxPrefix)
            return cidr;
        if(System.Net.IPAddress.TryParse(parts[0], out var ip))
        {
            if(ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return cidr; // for non-IPv4, no adjustment
            var ipBytes = ip.GetAddressBytes();
            // Convert bytes to uint in big-endian order.
            uint ipUint = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) | ((uint)ipBytes[2] << 8) | ipBytes[3];
            // Compute new mask: (32 - maxPrefix) zeros.
            uint mask = maxPrefix == 0 ? 0u : 0xFFFFFFFF << (32 - maxPrefix);
            uint network = ipUint & mask;
            var networkBytes = new byte[] {
                (byte)((network >> 24) & 0xFF),
                (byte)((network >> 16) & 0xFF),
                (byte)((network >> 8) & 0xFF),
                (byte)(network & 0xFF)
            };
            var networkIp = new System.Net.IPAddress(networkBytes);
            return networkIp.ToString() + "/" + maxPrefix;
        }
        return cidr;
    }
}