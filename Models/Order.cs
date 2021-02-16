using System;
using System.Collections.Generic;
using System.Linq;
using Dapper.Contrib.Extensions;

namespace DapperFiddle.Models
{
    public class Order
    {
        private readonly List<OrderLine> _orderLines = new();

        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; private set; }
        public string Customer { get; set; }
        public string Remarks { get; set; }

        [Computed]
        public bool IsNew => Id == default;

        [Write(false)]
        public IEnumerable<OrderLine> OrderLines => _orderLines;


        public void Add(OrderLine orderLine)
        {
            _orderLines.Add(orderLine);
            Amount += orderLine.Amount;
        }

        public void Add(List<OrderLine> orderLines)
        {
            _orderLines.AddRange(orderLines);
            Amount += orderLines.Sum(orderLine => orderLine.Amount);
        }
    }
}