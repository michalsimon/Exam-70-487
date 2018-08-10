namespace WebAPIDesign.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;

    using WebAPIDesign.Models;

    public class CustomerController : ApiController
    {
        public IEnumerable<Customer> GetCustomers(int accountId)
        {
            return DataRepository.Customers.Where(
                cust =>
                    cust.AccountId == accountId);
        }

        public IEnumerable<Customer> SearchCustomers(string lastName)
        {
            return DataRepository.Customers.Where(
                cust =>
                    cust.LastName.ToLower().Contains(lastName.ToLower()));
        }
    }
}