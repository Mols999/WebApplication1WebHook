using System;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace WebApplication1WebHook.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";
            return View();
        }

        // Method to create a new customer using a SOAP
        [HttpPost]
        public async Task<ActionResult> CreateCustomer(string username, string email, string firstName, string lastName)
        {
            string serviceUrl = "http://bc-container:7047/BC/WS/CRONUS%20Danmark/Codeunit/CustomerManagement";
            string soapAction = "urn:microsoft-dynamics-schemas/codeunit/CustomerManagement:CreateCustomer";

            string userName = "ADMIN";
            string password = "Password";

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly)
            {
                Security = { Transport = { ClientCredentialType = HttpClientCredentialType.Basic } },
                MaxReceivedMessageSize = 65536
            };

            var endpoint = new EndpointAddress(serviceUrl);

            using (var factory = new ChannelFactory<IWCFService>(binding, endpoint))
            {
                factory.Credentials.UserName.UserName = userName;
                factory.Credentials.UserName.Password = password;

                var client = factory.CreateChannel();

                var customerData = new
                {
                    name = $"{firstName} {lastName}".Trim(),
                    email = email,
                    username = username
                };

                var jsonContent = JsonConvert.SerializeObject(customerData);

                using (new OperationContextScope((IContextChannel)client))
                {
                    HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();
                    requestMessage.Headers["SOAPAction"] = $"\"{soapAction}\"";
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                    bool result = client.CreateCustomer(jsonContent);
                    if (result)
                    {
                        return Content("CreateCustomer call result: Success");
                    }
                    else
                    {
                        return Content("CreateCustomer call result: Failed");
                    }
                }
            }
        }

        // Method to create a new sales order using a SOAP
        [HttpPost]
        public async Task<ActionResult> CreateSalesOrder(string customerEmail, string productNumber, decimal quantity)
        {
            string serviceUrl = "http://bc-container:7047/BC/WS/CRONUS%20Danmark/Codeunit/SalesOrderManagement";
            string soapAction = "urn:microsoft-dynamics-schemas/codeunit/SalesOrderManagement:CreateSalesOrder";

            string userName = "ADMIN";
            string password = "Password";

            var salesOrderData = new
            {
                customerEmail = customerEmail,
                productNumber = productNumber,
                quantity = quantity
            };

            var jsonContent = JsonConvert.SerializeObject(salesOrderData);

            string soapEnvelope = $@"
            <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:urn='urn:microsoft-dynamics-schemas/codeunit/SalesOrderManagement'>
               <soapenv:Header/>
               <soapenv:Body>
                  <urn:CreateSalesOrder>
                     <urn:salesOrderJson>{System.Security.SecurityElement.Escape(jsonContent)}</urn:salesOrderJson>
                  </urn:CreateSalesOrder>
               </soapenv:Body>
            </soapenv:Envelope>";

            using (var httpClientHandler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) })
            using (var httpClient = new HttpClient(httpClientHandler))
            {
                var httpContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                httpContent.Headers.Add("SOAPAction", soapAction);

                var response = await httpClient.PostAsync(serviceUrl, httpContent);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content("CreateSalesOrder call result: Success");
                }
                else
                {
                    return Content("CreateSalesOrder call result: Failed");
                }
            }
        }

        // Interface for the CustomerManagement and SalesOrderManagement SOAP
        [ServiceContract(Namespace = "urn:microsoft-dynamics-schemas/codeunit/CustomerManagement")]
        public interface IWCFService
        {
            [OperationContract(Action = "urn:microsoft-dynamics-schemas/codeunit/CustomerManagement:CreateCustomer")]
            bool CreateCustomer(string customerJson);

            [OperationContract(Action = "urn:microsoft-dynamics-schemas/codeunit/SalesOrderManagement:CreateSalesOrder")]
            bool CreateSalesOrder(string salesOrderJson);
        }
    }
}
