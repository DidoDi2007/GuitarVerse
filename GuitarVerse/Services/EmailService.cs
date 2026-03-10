using GuitarVerse.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GuitarVerse.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        // Инжектираме IConfiguration, за да четем тайните
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            // Четем от User Secrets (или appsettings)
            string fromAddress = _configuration["EmailSettings:FromAddress"];
            string appPassword = _configuration["EmailSettings:AppPassword"];

            using (var client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(fromAddress, appPassword);

                var mailMessage = new MailMessage(fromAddress, toAddress, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
            }
        }

        public async Task SendOrderConfirmationAsync(string toEmail, Order order)
        {
            var subject = $"GuitarVerse Order Confirmation #{order.OrderID}";

            // Генерираме HTML таблица с продуктите
            var productsHtml = "";
            foreach (var item in order.OrderDetails)
            {
                productsHtml += $@"
            <tr>
                <td style='padding: 5px; border-bottom: 1px solid #ddd;'>{item.Product.Name} ({item.Product.Brand})</td>
                <td style='padding: 5px; border-bottom: 1px solid #ddd;'>{item.Quantity}</td>
                <td style='padding: 5px; border-bottom: 1px solid #ddd;'>{item.Price:N2} €</td>
            </tr>";
            }

                var body = $@"
            <div style='font-family: Arial, sans-serif; color: #333;'>
                <h2 style='color: #c0392b;'>Thank you for your order, {order.FirstName}!</h2>
                <p>Your order <strong>#{order.OrderID}</strong> has been placed successfully.</p>
                <p>We are getting your gear ready for shipment to:</p>
                <p><strong>{order.Address}, {order.City}, {order.Country}</strong></p>
            
                <h3>Order Summary</h3>
                <table style='width: 100%; border-collapse: collapse; text-align: left;'>
                    <thead>
                        <tr style='background-color: #f2f2f2;'>
                            <th style='padding: 10px;'>Product</th>
                            <th style='padding: 10px;'>Qty</th>
                            <th style='padding: 10px;'>Price</th>
                        </tr>
                    </thead>
                    <tbody>
                        {productsHtml}
                    </tbody>
                </table>
            
                <h3 style='text-align: right;'>Total: {order.TotalAmount:N2} €</h3>
                <hr>
                <p>Keep rocking,<br>The GuitarVerse Team</p>
            </div>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
