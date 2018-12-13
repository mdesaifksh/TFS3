using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using FirstKeyHomes.D365.Integration.PlugIns;
using System.ServiceModel.Description;
using System.Net;
using Microsoft.Xrm.Sdk.Client;

namespace AzureTest
{
    class Program
    {
        private static CrmServiceClient _client;
        private const string CRM_ORG_NAME = "firstkeyhomesdev";

        public static void Main(string[] args)
        {
            try
            {
                IOrganizationService _service = ConnectToCRM();

                    //Do stuff
                WhoAmIResponse res = (WhoAmIResponse)_service.Execute(new WhoAmIRequest());

                Console.WriteLine(res.UserId);
                //IOrganizationService _service = (IOrganizationService)_client.OrganizationWebProxyClient != null ? (IOrganizationService)_client.OrganizationWebProxyClient : (IOrganizationService)_client.OrganizationServiceProxy;

                string unsecureConfig = "Endpoint=sb://fsk-d365-integration.servicebus.windows.net/;SharedAccessKeyName=ListenAndSend;SharedAccessKey=AcwFsm2HmQv9pBBXEPVKl8xBETXnWuaUYBDS8GMMA3k=";
                string secureConfig = string.Empty;

                Entity entity = _service.Retrieve("po_unit", new Guid("6068F309-8A9D-E811-A857-000D3A14019B"), new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                PropertyToAZRServiceBus pta = new PropertyToAZRServiceBus(unsecureConfig,secureConfig);
                pta.ExecuteMessage(_service, entity);


            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                string message = ex.Message;
                throw;
            }
        }

        public static IOrganizationService ConnectToCRM()
        {
            //IOrganizationService organizationService = null;

            try
            {
                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = "mdesai@firstkeyhomes.com";
                clientCredentials.UserName.Password = "F@ll20!8s";

                // For Dynamics 365 Customer Engagement V9.X, set Security Protocol as TLS12
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                // Get the URL from CRM, Navigate to Settings -> Customizations -> Developer Resources
                // Copy and Paste Organization Service Endpoint Address URL
                IOrganizationService organizationService = (IOrganizationService)new OrganizationServiceProxy(new Uri($"https://{CRM_ORG_NAME}.api.crm.dynamics.com/XRMServices/2011/Organization.svc"),
                 null, clientCredentials, null);

                if (organizationService != null)
                {
                    Guid userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;

                    if (userid != Guid.Empty)
                    {
                        Console.WriteLine($"Connection Established Successfully...{userid.ToString()}");
                    }
                    return organizationService;
                }
                else
                {
                    Console.WriteLine("Failed to Established Connection!!!");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught - " + ex.Message);
                return null;
            }
        }
    }
}