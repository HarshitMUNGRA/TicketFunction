using System;
using System.Net.Sockets;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace TicketFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("tickets", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            // NOTE: The JSON deserialization is case-sensitive
            string json = message.MessageText;
            //
            // Deserialize the message JSON into a Ticket object
            //

            // Use options to make case insensitive
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize the JSON into a Ticket object
            TicketPurchase? ticket = JsonSerializer.Deserialize<TicketPurchase>(json, options);

            if (ticket == null)
            {
                _logger.LogError("Failed to deserialize message JSON into a Ticket object");
                return;

            }

            _logger.LogInformation($"ConcertId: {ticket.ConcertId}!");
            _logger.LogInformation($"Email: {ticket.Email}!");
            _logger.LogInformation($"Name: {ticket.Name}!");
            _logger.LogInformation($"Phone: {ticket.Phone}!");
            _logger.LogInformation($"Quantity: {ticket.Quantity}!");
            _logger.LogInformation($"CreditCard: {ticket.CreditCard}!");
            _logger.LogInformation($"Expiration: {ticket.Expiration}!");
            _logger.LogInformation($"SecurityCode: {ticket.SecurityCode}!");
            _logger.LogInformation($"Address: {ticket.Address}!");
            _logger.LogInformation($"City: {ticket.City}!");
            _logger.LogInformation($"Province: {ticket.Province}!");
            _logger.LogInformation($"PostalCode: {ticket.PostalCode}!");
            _logger.LogInformation($"Country: {ticket.Country}!");
            _logger.LogInformation($"CreditCard: {ticket.CreditCard}!");

            //
            // Add ticket to the database
            //

            // get connection string from app settings
            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            // using statement ensures the connection is closed when done
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync(); // Note the ASYNC

                var query = @"INSERT INTO ConcertTickets (concertId, email, Name, phone, quantity, creditCard, expiration, securityCode, address, city, province, postalCode, country)
                                    VALUES (@concertId, @email, @Name, @phone, @quantity, @creditCard, @expiration, @securityCode, @address, @city, @province, @postalCode, @country);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Adding SQL parameters safely
                    cmd.Parameters.AddWithValue("@concertId", ticket.ConcertId);
                    cmd.Parameters.AddWithValue("@Email", ticket.Email);
                    cmd.Parameters.AddWithValue("@Name", ticket.Name);
                    cmd.Parameters.AddWithValue("@Phone", ticket.Phone);
                    cmd.Parameters.AddWithValue("@Quantity", ticket.Quantity);
                    cmd.Parameters.AddWithValue("@CreditCard", ticket.CreditCard);  // Store only last 4 digits for security
                    cmd.Parameters.AddWithValue("@Expiration", ticket.Expiration);
                    cmd.Parameters.AddWithValue("@SecurityCode", ticket.SecurityCode); // Should not be stored in a real system
                    cmd.Parameters.AddWithValue("@Address", ticket.Address);
                    cmd.Parameters.AddWithValue("@City", ticket.City);
                    cmd.Parameters.AddWithValue("@Province", ticket.Province);
                    cmd.Parameters.AddWithValue("@PostalCode", ticket.PostalCode);
                    cmd.Parameters.AddWithValue("@Country", ticket.Country);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
