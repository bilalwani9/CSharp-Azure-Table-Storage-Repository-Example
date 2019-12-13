﻿namespace AzureFuns.Data.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;

    public class Employee : TableEntity
    {
        public Employee(string partitionKey, Guid guidId)
        {
            PartitionKey = partitionKey;
            RowKey = guidId.ToString();
            Id = guidId;
        }

        public Guid Id { get; private set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string MobileNo { get; set; }
        public string ICNumber { get; set; }
        public DateTime Dob { get; set; }
        public double Salary { get; set; }
        public string Address { get; set; }
    }
}
