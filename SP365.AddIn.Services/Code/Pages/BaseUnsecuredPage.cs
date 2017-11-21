namespace SP365.AddIn.Services
{
    public class BaseUnsecuredPage : BasePage
    {
        protected override bool AllowAnonymousQ { get { return true; } }
    }
}
