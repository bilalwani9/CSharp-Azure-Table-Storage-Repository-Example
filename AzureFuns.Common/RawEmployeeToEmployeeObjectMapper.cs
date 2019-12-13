using AzureFuns.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureFuns.Common
{
    using static AzureFuns.Common.Constants;
    public class RawEmployeeToEmployeeObjectMapper : IMapper<RawEmployee, Employee>
    {
        public Employee Map(RawEmployee rawEmployee)
        {
            return new Employee(TablePartitionKey, rawEmployee.Id)
            {
                 Address = rawEmployee.Address,
                 Dob = rawEmployee.Dob,
                 FirstName = rawEmployee.FirstName,
                 ICNumber = rawEmployee.ICNumber,
                 LastName = rawEmployee.LastName,
                 MiddleName = rawEmployee.MiddleName,
                 MobileNo = rawEmployee.MobileNo,
                 Salary = rawEmployee.Salary 
            };
        }
    }
}
