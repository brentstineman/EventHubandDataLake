using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signature_Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo enUS = new CultureInfo("en-US");

            Console.WriteLine("What is your service bus namespace? (do not include the 'servicebus.windows.net' part)");
            string sbNamespace = Console.ReadLine().Trim();

            Console.WriteLine("What is the path within the namespace you want to secure?");
            string sbPath = Console.ReadLine().Trim();

            Console.WriteLine("What is the name of the signing policy?");
            string sbPolicy = Console.ReadLine().Trim();

            Console.WriteLine("What is the policy's secret key?");
            string sbKey = Console.ReadLine().Trim();

            DateTime tmpDT = DateTime.UtcNow;
            bool gotDate = false; 
            while (!gotDate)
            {
                Console.WriteLine("When should this expire GMT? Use format 'MM/DD/YY HH'");
                string sbExpiry = Console.ReadLine().Trim();

                // convert the string into a timespan... 
                gotDate = DateTime.TryParseExact(sbExpiry, "M/dd/yy HH", enUS, DateTimeStyles.None, out tmpDT);
                if (!gotDate)
                    Console.WriteLine("'{0}' is not in an acceptable format.", sbExpiry);
            }

            //TimeSpan expiry = DateTime.UtcNow.Subtract(tmpDT);
            TimeSpan expiry = tmpDT.Subtract(DateTime.UtcNow);

            var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", sbNamespace, sbPath).ToString().Trim('/');
            string generatedSaS = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(sbPolicy, sbKey, serviceUri, expiry);

            Console.WriteLine("Your signature is: {0}", generatedSaS);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
