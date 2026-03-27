using AgileLabs.WebApp;
using Microsoft.AspNetCore.Mvc;

namespace AgentMailbox.WebApis;

public sealed class AgentMailboxAppOptions : DefaultMvcApplicationOptions
{
    public AgentMailboxAppOptions()
    {
        TypeFinderAssemblyScanPattern = "^AgentMailbox|^AgileLabs";
        MvcBuilderCreateFunc = static (serviceCollection, action) =>
            serviceCollection.AddControllers(action);
    }
}
