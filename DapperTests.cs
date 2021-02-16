using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using DapperFiddle.Infrastructure;
using DapperFiddle.Models;
using NUnit.Framework;
using Shouldly;
using YamlDotNet.Serialization;

namespace DapperFiddle
{
    public class DapperTests
    {
        private Order _order;

        [OneTimeSetUp]
        public void Setup()
        {
            LocalDb.Create();
            LocalDb.Migrate();

            _order = new Order { Date = DateTime.Today, Customer = "Homer", Remarks = "Test Order" };
        }

        [Test, Order(1)]
        public async Task InsertOrderTheHardWay()
        {
            const string insertQuery = @"INSERT INTO [dbo].[Orders](Date, Amount, Customer, Remarks) OUTPUT INSERTED.Id VALUES (@Date, @Amount, @Customer, @Remarks)";

            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            (await connection.QuerySingleAsync<long>(insertQuery, _order)).ShouldBe(1);
            _order.Id.ShouldBe(0);
            (await connection.QuerySingleAsync<long>(insertQuery, _order)).ShouldBe(2);
            _order.Id.ShouldBe(0);
        }

        [Test, Order(2)]
        public async Task InsertOrderTheEasyWay()
        {
            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            (await connection.InsertAsync(_order)).ShouldBe(3);
            _order.Id.ShouldBe(3);
        }

        [Test, Order(3)]
        public async Task GetOrderTheHardWay()
        {
            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            var oder = (await connection.QueryAsync<Order>(@"SELECT Id, Date, Amount, Customer, Remarks FROM [dbo].[Orders] WHERE Id = @Id", new { Id = 3 })).Single();
            oder.ShouldBeEquivalentTo(_order);
        }

        [Test, Order(4)]
        public async Task GetOrderTheEasyWay()
        {
            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            var oder = await connection.GetAsync<Order>(3); // Will perform select * :(
            oder.ShouldBeEquivalentTo(_order);
        }

        [Test, Order(5)]
        public async Task InsertOrderLinesTheHardWay()
        {
            const string insertQuery = @"INSERT INTO [dbo].[OrderLines](OrderId, Product, Amount) VALUES (@OrderId, @Product, @Amount)";
            
            _order.Add(new OrderLine { Id = 1, OrderId = 3, Amount = 1, Product = "A" });
            _order.Add(new OrderLine { Id = 2, OrderId = 3, Amount = 2, Product = "B" });
            _order.Amount.ShouldBe(3);

            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            await connection.ExecuteAsync(insertQuery, _order.OrderLines);
        }
        [Test, Order(6)]
        public async Task InsertOrderLinesTheEasyWay()
        {
            _order.Add(new OrderLine { OrderId = 3, Amount = 3, Product = "C" });
            _order.Add(new OrderLine { OrderId = 3, Amount = 4, Product = "D" });
            _order.Amount.ShouldBe(10);

            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            
            foreach (var orderLine in _order.OrderLines.Skip(2))
            {
                await connection.InsertAsync(orderLine);
            }
        }

        [Test, Order(7)]
        public async Task GetFullOrder()
        {
            const string sql = "SELECT * FROM Orders WHERE Id = @Id; SELECT * FROM OrderLines WHERE OrderId = @Id";
            await using var connection = new SqlConnection(LocalDb.ConnectionString);

            using var multipleResults = await connection.QueryMultipleAsync(sql, new { Id = 3 });
            var order = (await multipleResults.ReadAsync<Order>()).Single();
            var orderLines = (await multipleResults.ReadAsync<OrderLine>()).ToList();
            order.Add(orderLines);

            order.ShouldBeEquivalentTo(_order);
            order.Amount.ShouldBe(10);
            order.OrderLines.Count().ShouldBe(4);
        }

        [Test, Order(8)]
        public async Task GetFullOrderWithInnerJoin()
        {
            const string sql = "SELECT * FROM Orders O INNER JOIN OrderLines OL ON OL.OrderId = O.Id WHERE O.Id = @Id";
            await using var connection = new SqlConnection(LocalDb.ConnectionString);

            var ordersDictionary = new Dictionary<int, Order>();

            var orders = (await connection.QueryAsync<Order, OrderLine, Order>(sql, (order, orderLine) =>
            {
                if (!ordersDictionary.TryGetValue(order.Id, out var currentOrder))
                {
                    currentOrder = order;
                    ordersDictionary.Add(currentOrder.Id, currentOrder);
                }
                currentOrder.Add(orderLine);
                return currentOrder;
            }, new { Id = 3 })).Distinct().ToList();

            orders.Count.ShouldBe(1);
            orders.Single().OrderLines.Count().ShouldBe(4);
        }

        [Test, Order(9)]
        public async Task ShowWhatsInTheDatabase()
        {
            await using var connection = new SqlConnection(LocalDb.ConnectionString);
            var orders = (await connection.GetAllAsync<Order>()).ToList();
            var orderLines = (await connection.GetAllAsync<OrderLine>()).ToList();

            foreach (var order in orders)
            {
                order.Add(orderLines.Where(ol => ol.OrderId == order.Id).ToList());
            }

            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(orders);
            Console.WriteLine(yaml);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            LocalDb.Drop();
            LocalDb.Stop();
        }
    }
}