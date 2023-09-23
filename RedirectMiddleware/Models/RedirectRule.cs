namespace RedirectMiddleware.Models;

internal class RedirectRule
{
    public string RedirectUrl { get; set; }
    public string TargetUrl { get; set; }
    public int RedirectType { get; set; }
    public bool UseRelative { get; set; }
}