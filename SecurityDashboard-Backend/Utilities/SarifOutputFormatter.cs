using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CodeAnalysis.Sarif;
using System.Text;

namespace SecurityDashboard_Backend.Utilities;

public class SarifOutputFormatter : TextOutputFormatter
{
    public SarifOutputFormatter()
    {
        // SarifLog will be returned as application/sarif+json in UTF-8.
        SupportedMediaTypes.Add("application/sarif+json");
        SupportedMediaTypes.Add("application/json");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    [ExcludeFromCodeCoverage(Justification = "Just a condition check")]
    protected override bool CanWriteType(Type? type)
    {
        // Allow only SarifLog to be written.
        return type == typeof(SarifLog);
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var log = (SarifLog?)context.Object;
        if (log == null)
            return Task.CompletedTask;

        // Sarif.SDK does not allow async saving of SarifLog to stream.
        // Temporarily allow sync IO to save the SarifLog.
        var syncIoFeature = context.HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (syncIoFeature != null)
            syncIoFeature.AllowSynchronousIO = true;

        log.Save(context.HttpContext.Response.Body);

        // Revert sync IO to its original state.
        if (syncIoFeature != null)
            syncIoFeature.AllowSynchronousIO = false;

        return Task.CompletedTask;
    }
}
