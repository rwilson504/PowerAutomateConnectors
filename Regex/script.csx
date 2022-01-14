public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Check if the operation ID matches what is specified in the OpenAPI definition of the connector
        switch (this.Context.OperationId)
        {
            case "RegexIsMatch":
                return await this.HandleRegexIsMatchOperation().ConfigureAwait(false);
            case "GetRegexMatch":
                return await this.HandleGetRegexMatchOperation().ConfigureAwait(false);
            case "GetRegexMatches":
                return await this.HandleGetRegexMatchesOperation().ConfigureAwait(false);
        }

        // Handle an invalid operation ID
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        response.Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
        return response;
    }

    private RegexOptions GetRegexOptions(JObject contentAsJson)
    {
        int regexOptionsMask = 0;

        if ((bool?)contentAsJson["CultureInvariant"] == true) regexOptionsMask += (int)RegexOptions.CultureInvariant;
        if ((bool?)contentAsJson["ECMAScript"] == true) regexOptionsMask += (int)RegexOptions.ECMAScript;
        if ((bool?)contentAsJson["ExplicitCapture"] == true) regexOptionsMask += (int)RegexOptions.ExplicitCapture;
        if ((bool?)contentAsJson["IgnoreCase"] == true) regexOptionsMask += (int)RegexOptions.IgnoreCase;
        if ((bool?)contentAsJson["IgnorePatternWhitespace"] == true) regexOptionsMask += (int)RegexOptions.IgnorePatternWhitespace;
        if ((bool?)contentAsJson["Multiline"] == true) regexOptionsMask += (int)RegexOptions.Multiline;
        if ((bool?)contentAsJson["RightToLeft"] == true) regexOptionsMask += (int)RegexOptions.RightToLeft;
        if ((bool?)contentAsJson["Singleline"] == true) regexOptionsMask += (int)RegexOptions.Singleline;

        return (RegexOptions)regexOptionsMask;
    }

    private async Task<HttpResponseMessage> HandleRegexIsMatchOperation()
    {
        HttpResponseMessage response;
            
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Parse as JSON object
        var contentAsJson = JObject.Parse(contentAsString);

        // Get the value of text to check
        var textToCheck = (string)contentAsJson["textToCheck"];

        // Create a regex based on the request content
        var regexInput = (string)contentAsJson["regex"];
            
        // Get Regex Options
        var regexOps = GetRegexOptions(contentAsJson);

        JObject output = new JObject
        {
            ["textToCheck"] = textToCheck,
            ["isMatch"] = Regex.IsMatch(textToCheck, regexInput, regexOps)
        };

        response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CreateJsonContent(output.ToString());
        return response;
    }

    private async Task<HttpResponseMessage> HandleGetRegexMatchOperation()
    {
        HttpResponseMessage response;

        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Parse as JSON object
        var contentAsJson = JObject.Parse(contentAsString);

        // Get the value of text to check
        var textToCheck = (string)contentAsJson["textToCheck"];

        // Create a regex based on the request content
        var regexInput = (string)contentAsJson["regex"];

        // Get Regex Options
        var regexOps = GetRegexOptions(contentAsJson);

        Match match = Regex.Match(textToCheck, regexInput, regexOps);
        JObject output = new JObject
        {
            ["textToCheck"] = textToCheck,
            ["match"] = match.Success ? match.Value : "",
            ["position"] = match.Success ? match.Index : -1
        };

        response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CreateJsonContent(output.ToString());
        return response;
    }

    private async Task<HttpResponseMessage> HandleGetRegexMatchesOperation()
    {
        HttpResponseMessage response;

        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Parse as JSON object
        var contentAsJson = JObject.Parse(contentAsString);

        // Get the value of text to check
        var textToCheck = (string)contentAsJson["textToCheck"];

        // Create a regex based on the request content
        var regexInput = (string)contentAsJson["regex"];            

        // Get Regex Options
        var regexOps = GetRegexOptions(contentAsJson);

        // Get Regex matches
        var matches = Regex.Matches(textToCheck, regexInput, regexOps).Cast<Match>();
        var matchCount = matches.Count();        

        JObject output = JObject.FromObject(new
        {
            textToCheck = textToCheck,
            matchCount = matchCount,
            matches =
                from m in matches
                select new
                {
                    value = m.Value,
                    position = m.Index,
                    groups =
                        from g in m.Groups.Cast<Group>()
                        select new
                        {
                            name = g.Name,
                            value = g.Value,
                            captures =
                                from c in g.Captures.Cast<Capture>()
                                select new
                                { 
                                    value = c.Value,
                                    position = c.Index
                                }
                        }
                }
        });       

        response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CreateJsonContent(output.ToString());
        return response;
    }
}