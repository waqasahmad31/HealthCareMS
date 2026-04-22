using System.Net.Http.Headers;
using System.Text;
using HealthCareMS.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Infrastructure.Notifications;

public sealed class TwilioSmsSender(
    HttpClient httpClient,
    IOptions<TwilioOptions> options,
    ILogger<TwilioSmsSender> logger) : ISmsSender
{
    public async Task<DeliveryResult> SendAsync(string destination, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return DeliveryResult.Failed("SMS destination is empty.");
        }

        var twilioOptions = options.Value;
        if (!twilioOptions.Enabled)
        {
            logger.LogInformation("Twilio disabled; simulated SMS to {Destination}.", destination);
            return DeliveryResult.Sent;
        }

        if (string.IsNullOrWhiteSpace(twilioOptions.AccountSid)
            || string.IsNullOrWhiteSpace(twilioOptions.AuthToken)
            || string.IsNullOrWhiteSpace(twilioOptions.FromNumber))
        {
            return DeliveryResult.Failed("Twilio configuration is incomplete.");
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{twilioOptions.AccountSid}/Messages.json");

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{twilioOptions.AccountSid}:{twilioOptions.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["From"] = twilioOptions.FromNumber,
            ["To"] = destination,
            ["Body"] = body
        });

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return DeliveryResult.Sent;
            }

            var failure = await response.Content.ReadAsStringAsync(cancellationToken);
            return DeliveryResult.Failed(failure);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Twilio SMS delivery failed for {Destination}.", destination);
            return DeliveryResult.Failed(ex.Message);
        }
    }
}
