using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CodeAnalysis.Sarif;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SecurityDashboard_Backend.Utilities;

public class SarifInputFormatter : TextInputFormatter
{
    public SarifInputFormatter()
    {
        SupportedMediaTypes.Add("application/sarif+json");
        SupportedEncodings.Add(UTF16EncodingLittleEndian);
        SupportedEncodings.Add(UTF8EncodingWithoutBOM);
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    [ExcludeFromCodeCoverage(Justification = "Just a condition check")]
    protected override bool CanReadType(Type type)
    {
        // Allow any string or SarifLog (if parsing was already tried) to be read.
        // Requires Content-Type header to be set to "application/sarif+json".
        return type == typeof(string) || type == typeof(SarifLog);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        // Sarif.SDK does not allow async loading of SarifLog from stream.
        // Temporarily allow sync IO to load the SarifLog.
        var syncIoFeature = context.HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (syncIoFeature != null)
            syncIoFeature.AllowSynchronousIO = true;

        try
        {
            var sarifLog = SarifLog.Load(context.HttpContext.Request.Body);

            if (sarifLog == null)
                return await InputFormatterResult.NoValueAsync();

            return await InputFormatterResult.SuccessAsync(sarifLog);
        }
        catch
        {
            return await InputFormatterResult.FailureAsync();
        }
        finally
        {
            // Revert sync IO to its original state.
            if (syncIoFeature != null)
                syncIoFeature.AllowSynchronousIO = false;
        }
    }
}
