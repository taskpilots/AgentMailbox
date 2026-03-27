using AgileLabs;
using AgileLabs.AppRegisters;
using AgileLabs.Json;
using AgileLabs.WebApp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMailbox.WebApis;

public sealed class AppConfigure : IServiceRegister, IRequestPiplineRegister, IMvcBuildConfig, IEndpointConfig
{
    public void ConfigureServices(IServiceCollection services, AppBuildContext buildContext)
    {
        services.AddAgentMailboxApplicationServices(buildContext.Configuration);
    }

    public RequestPiplineCollection Configure(RequestPiplineCollection pipelineActions, AppBuildContext buildContext)
    {
        pipelineActions.Register("StaticFiles", RequestPipelineStage.BeforeRouting, app =>
        {
            app.UseStaticFiles();
        });

        return pipelineActions;
    }

    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder, AppBuildContext appBuildContext)
    {
        mvcBuilder.AddNewtonsoftJson(jsonOptions =>
        {
            JsonNetSerializerSettings.DecorateCamelCaseSerializerSettings(jsonOptions.SerializerSettings);
        });
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints, AppBuildContext appBuildContext)
    {
        endpoints.MapControllers();
        endpoints.MapHealthChecks("/health");
    }
}
