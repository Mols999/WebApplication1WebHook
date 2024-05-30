using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1WebHook.Controllers;

namespace WebApplication1WebHook
{
    // Class to handle JSON webhooks
    public class GenericJsonWebHookHandler : WebHookHandler
    {
        public GenericJsonWebHookHandler()
        {
            this.Receiver = "genericjson";
        }

        // Method to run when a webhook is received
        public override async Task ExecuteAsync(string receiver, WebHookHandlerContext context)
        {
            JObject data = context.GetDataOrDefault<JObject>();

            try
            {
                // Get the webhook topic from the request headers
                string topic = context.Request.Headers.GetValues("X-WC-Webhook-Topic").FirstOrDefault();

                HomeController controller = new HomeController();

                // Handle customer created
                if (topic != null && topic.ToLower().Equals("customer.created"))
                {
                    dynamic dData = data;
                    string username = dData.username;
                    string email = dData.email;
                    string firstName = dData.first_name;
                    string lastName = dData.last_name;

                    await controller.CreateCustomer(username, email, firstName, lastName);
                }
                // Handle order created
                else if (topic != null && topic.ToLower().Equals("order.created"))
                {
                    dynamic dData = data;
                    string customerEmail = dData.billing.email;
                    string productNumber = dData.line_items[0].product_id.ToString();
                    decimal quantity = dData.line_items[0].quantity;

                    await controller.CreateSalesOrder(customerEmail, productNumber, quantity);
                }
            }
            catch (Exception ex)
            {
                // Debug
                System.Diagnostics.Debug.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}
