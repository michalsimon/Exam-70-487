namespace WebAPIDesign.Models
{
    using System;

    public class Account
    {
        public int AccountId { get; set; }
        public string AccountAlias { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}