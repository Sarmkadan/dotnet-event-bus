using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace WebhookHandler
{
    public class WebhookHandler
    {
        public void HandleWebhook(string payload)
        {
            // Check if payload is null or empty
            if (string.IsNullOrEmpty(payload))
            {
                throw new ArgumentNullException(nameof(payload));
            }

            // Check if payload exceeds the maximum allowed size
            if (payload.Length > 1024 * 1024)
            {
                throw new ArgumentException("Payload exceeds the maximum allowed size.", nameof(payload));
            }

            // Deserialize the payload using a safe, type-constrained method
            var payloadObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);

            // Process the payload object
            // ...
        }
    }
}