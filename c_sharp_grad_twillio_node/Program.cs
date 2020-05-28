using Nito.AsyncEx;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace c_sharp_grad_twillio_node
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "donation",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());
                Console.WriteLine(" [x] Received {0}", message);

                //Extract data

                string[] words = message.Split(',');

                var cell = "+27" + words[0].Substring(1, 9);
                var smsbody = String.Format("Hello {0}, thank you for donating R{1} to help the COVID-19 cause. The Donate Team", words[2], words[1]);

                //Twillio
                var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("To", cell));
                nvc.Add(new KeyValuePair<string, string>("From", "+17748477045"));
                nvc.Add(new KeyValuePair<string, string>("Body", smsbody));

                var httpClient = new HttpClient();
                var encoding = new ASCIIEncoding();
                var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(encoding.GetBytes(string.Format("{0}:{1}", "ACfa70b55ad0f6e2fd05d0d41f5f6c8931", "2e1201887185d89077b3ab4b6659b7cb"))));
                httpClient.DefaultRequestHeaders.Authorization = authHeader;

                var req = new HttpRequestMessage(HttpMethod.Post, "https://api.twilio.com/2010-04-01/Accounts/ACfa70b55ad0f6e2fd05d0d41f5f6c8931/Messages.json") { Content = new FormUrlEncodedContent(nvc) };
                var response = await httpClient.SendAsync(req);

                Console.WriteLine("SMS: " + smsbody);
                Console.WriteLine("Sending SMS to user...");
            };

            channel.BasicConsume(queue: "donation",
                                     autoAck: true,
                                     consumer: consumer);
            Console.ReadLine();

        }      
        }
    }

