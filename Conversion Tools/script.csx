public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        var _logger = this.Context.Logger;
        _logger.LogInformation($"Operation ID: {this.Context.OperationId}");
        
        switch (this.Context.OperationId)
        {
            case "convertHtmlToText":
                return await this.HandleConvertHtmlToText().ConfigureAwait(false);
            default:
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
                return response;
        }
    }
    
    // New function for Convert HTML to text action
    private async Task<HttpResponseMessage> HandleConvertHtmlToText()
    {
        var query = System.Web.HttpUtility.ParseQueryString(this.Context.Request.RequestUri.Query);
        bool preserveLineBreaks = bool.TryParse(query["preserveLineBreaks"], out var plb) && plb;
        bool preserveWhitespace = bool.TryParse(query["preserveWhitespace"], out var pw) && pw;
        bool removeExtraSpaces = string.IsNullOrEmpty(query["removeExtraSpaces"]) ? true : bool.TryParse(query["removeExtraSpaces"], out var res) && res;
        
        string htmlBody = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        string text = htmlBody;
        
        if (preserveLineBreaks)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</p>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<p[^>]*>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        
        if (removeExtraSpaces)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        }
        else if (!preserveWhitespace)
        {
            text = text.Trim();
        }
        
        text = text.Trim();
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(text, System.Text.Encoding.UTF8, "text/plain");
        return response;
    }
}