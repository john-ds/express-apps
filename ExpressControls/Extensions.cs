namespace ExpressControls
{
    public static class StringExtensions
    {
        public static string Or(this string str, object? alternative)
        {
            return string.IsNullOrEmpty(str) ? alternative?.ToString() ?? "" : str;
        }
    }
}
